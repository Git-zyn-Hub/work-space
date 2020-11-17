using BrokenRail3MonitorViaWiFi.Windows;
using BrokenRailMonitorViaWiFi.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;

namespace BrokenRail3MonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly int MasterControlWidth = 26;
        private static readonly int RailWidth = 104;
        private static readonly int LeftOffset = 30;//主控添加应力之后，最左边的显示不全，所以Rail以及MasterControl整体右移。
        //private Window _container;
        private ScrollViewerThumbnail _svtThumbnail;
        private string _directoryName;
        private string _directoryHistoryName;
        private Socket _socketMain;
        private Socket _acceptSocket;
        private Thread _socketListeningThread;
        private Thread _socketFileRecvThread;
        private List<MasterControl> _masterControlList = new List<MasterControl>();
        private List<Rail> _rail1List = new List<Rail>();
        private List<Rail> _rail2List = new List<Rail>();
        private DispatcherTimer _getAllRailInfoTimer = new DispatcherTimer();
        private DispatcherTimer _waitReceiveTimer = new DispatcherTimer();
        private DispatcherTimer _multicastWaitReceiveTimer = new DispatcherTimer();
        private DispatcherTimer _timeToWaitTimer = new DispatcherTimer();
        private int _packageCount = 0;
        private int _receiveEmptyPackageCount = 0;
        private List<int> _socketRegister = new List<int>();
        private List<int> _4GPointIndex = new List<int>();
        private Dictionary<int, bool> _terminalsReceiveFlag;
        private List<int> _sendTime = new List<int>();
        private int _hit0xf4Count = 0;
        private List<string> _fileNameList = new List<string>();
        private bool _isConnect = false;
        private String _serverIP = "103.44.145.248";
        //private const String _serverWeb = "f1880f0253.51mypc.cn";
        private const String _serverWeb = "terrytec.iok.la";
        private const int _fileReceivePort = 23955;
        private bool _isSubscribingAllRailInfo = false;
        private Socket _socket;

        public List<int> SocketRegister
        {
            get
            {
                return _socketRegister;
            }

            set
            {
                _socketRegister = value;
            }
        }

        public List<MasterControl> MasterControlList
        {
            get
            {
                return _masterControlList;
            }

            set
            {
                _masterControlList = value;
            }
        }

        public DispatcherTimer WaitReceiveTimer
        {
            get
            {
                return _waitReceiveTimer;
            }

            set
            {
                _waitReceiveTimer = value;
            }
        }

        //public SerialPort SerialPort
        //{
        //    get { return _serialPort1; }
        //    set
        //    {
        //        if (_serialPort1 != value)
        //        {
        //            _serialPort1 = value;
        //        }
        //    }
        //}
        public MainWindow()
        {
            InitializeComponent();
            checkHistoryDirectory();
            //_getAllRailInfoTimer.Tick += getAllRailInfoTimer_Tick;
            //_getAllRailInfoTimer.Interval = new TimeSpan(0, 0, 75);

            WaitReceiveTimer.Tick += WaitReceiveTimer_Tick;
            WaitReceiveTimer.Interval = new TimeSpan(0, 0, 20);

            _multicastWaitReceiveTimer.Tick += multicastWaitReceiveTimer_Tick;
            _multicastWaitReceiveTimer.Interval = new TimeSpan(0, 0, 20);
            //_waitingRingThread = new Thread(waitingRingEnable);
        }

        private void devicesInitial()
        {
            try
            {
                foreach (var item in MasterControlList)
                {
                    item.Dispose();
                }
                this.MasterControlList.Clear();
                _4GPointIndex.Clear();
                _sendTime.Clear();
                string fileName = System.Environment.CurrentDirectory + @"\config.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);
                XmlNodeList xnList = xmlDoc.SelectSingleNode("Devices").ChildNodes;
                int nodeCount = xnList.Count;

                if (this.cvsRail1.Children.Count != 0 || this.cvsRail2.Children.Count != 0 || this.cvsDevices.Children.Count != 0)
                {
                    this.cvsRail1.Children.Clear();
                    this.cvsRail2.Children.Clear();
                    this.cvsDevices.Children.Clear();
                }

                int i = 0;
                int neighbourBigRemember = 0;
                foreach (XmlNode device in xnList)
                {
                    XmlNode terminalNoNode = device.SelectSingleNode("TerminalNo");
                    string innerTextTerminalNo = terminalNoNode.InnerText.Trim();
                    int terminalNo = Convert.ToInt32(innerTextTerminalNo);
                    MasterControl oneMasterControl = new MasterControl(this);
                    oneMasterControl.lblNumber.Content = terminalNo;
                    this.MasterControlList.Add(oneMasterControl);

                    //根据终端号计算发射占用无线串口的时机
                    int t = 4 + (terminalNo % 5) * 15;
                    if (!FindIntInSendTime(t))
                    {
                        _sendTime.Add(t);
                    }

                    XmlNode is4GNode = device.SelectSingleNode("Is4G");
                    string innerTextIs4G = is4GNode.InnerText.Trim();
                    bool is4G = Convert.ToBoolean(innerTextIs4G);
                    oneMasterControl.Is4G = is4G;
                    if (is4G)
                    {
                        _4GPointIndex.Add(this.MasterControlList.Count - 1);
                    }

                    Rail rail1 = new Rail(terminalNo);
                    Rail rail2 = new Rail(terminalNo);
                    //rail1.WhichRail = RailNo.Rail1;
                    //rail2.WhichRail = RailNo.Rail2;
                    this._rail1List.Add(rail1);
                    this._rail2List.Add(rail2);
                    this.cvsDevices.Children.Add(this.MasterControlList[this.MasterControlList.Count - 1]);
                    Canvas.SetLeft(this.MasterControlList[this.MasterControlList.Count - 1], (2 + RailWidth) * i + LeftOffset);
                    if (i < nodeCount - 1)
                    {
                        this.cvsRail1.Children.Add(rail1);
                        Canvas.SetLeft(rail1, (2 + RailWidth) * i + MasterControlWidth / 2 + 1 + LeftOffset);

                        this.cvsRail2.Children.Add(rail2);
                        Canvas.SetLeft(rail2, (2 + RailWidth) * i + MasterControlWidth / 2 + 1 + LeftOffset);
                    }
                    XmlNode neighbourSmallNode = device.SelectSingleNode("NeighbourSmall");
                    string innerTextNeighbourSmall = neighbourSmallNode.InnerText.Trim();
                    int neighbourSmall = Convert.ToInt32(innerTextNeighbourSmall);
                    XmlNode isEndNode = device.SelectSingleNode("IsEnd");
                    string innerTextIsEnd = isEndNode.InnerText.Trim();
                    bool isEnd = Convert.ToBoolean(innerTextIsEnd);
                    this.MasterControlList[this.MasterControlList.Count - 1].IsEnd = isEnd;

                    //检查工程文档配置文件是否正确
                    if (i == 0)
                    {
                        if (neighbourSmall != 0)
                        {
                            AppendMessage("第一个终端的NeighbourSmall标签未设置为0", DataLevel.Warning);
                        }
                    }
                    else
                    {
                        if (MasterControlList[i - 1].TerminalNumber != neighbourSmall)
                        {
                            AppendMessage("终端" + terminalNo.ToString() + "的小相邻终端不匹配，请检查配置文件", DataLevel.Warning);
                        }
                        if (oneMasterControl.TerminalNumber != neighbourBigRemember)
                        {
                            AppendMessage("终端" + MasterControlList[i - 1].TerminalNumber.ToString() + "的大相邻终端不匹配，请检查配置文件", DataLevel.Warning);
                        }
                    }
                    oneMasterControl.NeighbourSmall = neighbourSmall;
                    if (i >= 1)
                    {
                        MasterControlList[i - 1].NeighbourBig = neighbourBigRemember;
                    }
                    XmlNode neighbourBigNode = device.SelectSingleNode("NeighbourBig");
                    string innerTextNeighbourBig = neighbourBigNode.InnerText.Trim();
                    if (!isEnd)
                    {
                        int neighbourBig = Convert.ToInt32(innerTextNeighbourBig);
                        neighbourBigRemember = neighbourBig;
                        oneMasterControl.NeighbourBig = neighbourBig;
                    }

                    if (isEnd)
                    {
                        oneMasterControl.NeighbourBig = 0xff;
                        if (!(innerTextNeighbourBig == "ff" || innerTextNeighbourBig == "FF"))
                        {
                            AppendMessage("最末终端" + terminalNo.ToString() + "的大相邻终端不是ff，请检查配置文件", DataLevel.Warning);
                        }
                    }
                    i++;
                }
                this.cvsRail1.Width = (2 + RailWidth) * nodeCount;

                this._svtThumbnail = new ScrollViewerThumbnail(nodeCount - 1);
                this._svtThumbnail.ScrollViewerTotalWidth = (2 + RailWidth) * nodeCount;
                this._svtThumbnail.MouseClickedEvent += _svtThumbnail_MouseClickedEvent;
                this.gridMain.Children.Add(_svtThumbnail);
                this._svtThumbnail.SetValue(Grid.RowProperty, 2);
                this._svtThumbnail.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
                this._svtThumbnail.SetValue(MarginProperty, new Thickness(20, 0, 20, 0));
                //重新刷新之后需要清空Socket注册。
                SocketRegister.Clear();
            }
            catch (Exception ee)
            {
                AppendMessage("设备初始化异常：" + ee.Message, DataLevel.Error);
            }
        }

        public void AppendMessage(string msg, DataLevel level)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.dataShowUserCtrl.AddShowData(msg, level);
            }));
        }

        private void _svtThumbnail_MouseClickedEvent()
        {
            double offset = this._svtThumbnail.XPosition / this._svtThumbnail.CvsFollowMouseWidth * this._svtThumbnail.ScrollViewerTotalWidth;
            this.svContainer.ScrollToHorizontalOffset(offset);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            //if (_serialPort1.IsOpen)
            //{
            //    _serialPort1.Close();
            //}
        }
        //public void SerialPortInitialize()
        //{
        //    if (_serialPort1.IsOpen)
        //    {
        //        AppendMessage("串口早就打开了有木有!");
        //    }
        //    else
        //    {
        //        try
        //        {
        //            _serialPort1.Open();
        //            _serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);
        //            AppendMessage("端口打开！");
        //        }
        //        catch (Exception ee)
        //        {
        //            AppendMessage("端口无法打开! " + ee.Message);
        //        }
        //    }
        //}
        private void socketListening(object oSocket)
        {
            try
            {
                _socket = oSocket as Socket;
                if (_socket != null)
                {
                    int accumulateNumber = 0;
                    while (true)
                    {
                        byte[] receivedBytes = new byte[5120];
                        int numBytes = _socket.Receive(receivedBytes, SocketFlags.None);
                        //判断Socket连接是否断开
                        if (numBytes == 0)
                        {
                            if (_receiveEmptyPackageCount == 1)
                            {
                                connectCloseHandle(_socket);
                                break;
                            }
                            _receiveEmptyPackageCount++;
                        }
                        else
                        {
                            _receiveEmptyPackageCount = 0;
                        }
                        //收包指示交替变色，收到的包数自加1
                        _packageCount++;
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.lblPackageCount.Content = _packageCount.ToString();
                            bool isWhite = (this.elpIndicator.Fill as SolidColorBrush).Color.Equals(Colors.White);
                            bool isGreen = (this.elpIndicator.Fill as SolidColorBrush).Color.Equals(Colors.LightGreen);
                            if (isGreen)
                            {
                                this.elpIndicator.Fill = new SolidColorBrush(Colors.White);
                            }
                            else if (isWhite)
                            {
                                this.elpIndicator.Fill = new SolidColorBrush(Colors.LightGreen);
                            }
                        }));

                        byte[] actualReceive = new byte[numBytes];
                        for (int i = 0; i < numBytes; i++)
                        {
                            actualReceive[i] = receivedBytes[i];
                        }
                        //处理断包
                        //V519发满1024字节之后会截断一下，在下一个1024字节继续发送
                        //long beforePlusRemainder = accumulateNumber % 1024;
                        accumulateNumber += numBytes;
                        int afterPlusRemainder = accumulateNumber % 1024;
                        if (afterPlusRemainder == 0)
                        {
                            //等于0的时候说明接收的字段跨过1024字节，再收一组数据。
                            //有一种特殊情况，就是收到1024字节的时候正好是一整包，这样进入判断的话就会将两个本来就应该分开的包连起来，这种情况没有处理。
                            accumulateNumber = 0;
                            receivedBytes = new byte[2048];
                            numBytes = _socket.Receive(receivedBytes, SocketFlags.None);
                            accumulateNumber += numBytes;
                            byte[] secondReceive = new byte[numBytes];
                            for (int i = 0; i < numBytes; i++)
                            {
                                secondReceive[i] = receivedBytes[i];
                            }
                            byte[] sumReceive = new byte[actualReceive.Length + numBytes];
                            actualReceive.CopyTo(sumReceive, 0);
                            secondReceive.CopyTo(sumReceive, actualReceive.Length);
                            actualReceive = new byte[sumReceive.Length];
                            sumReceive.CopyTo(actualReceive, 0);
                            //this.Dispatcher.BeginInvoke(new Action(() =>
                            //{
                            //    this.dataShowUserCtrl.AddShowData("跨越1024字节处理！", DataLevel.Warning);
                            //}));
                        }

                        byte[] packageUnhandled = new byte[0];

                    handlePackage: ASCIIEncoding encoding = new ASCIIEncoding();
                        string strReceive = encoding.GetString(actualReceive);
                        if (strReceive.Length > 5)
                        {
                            string strReceiveFirst3Letter = strReceive.Substring(0, 3);
                            string strReceiveFirst6Letter = strReceive.Substring(0, 6);
                            //检查校验和
                            try
                            {
                                if (actualReceive[0] == 0x66 && actualReceive[1] == 0xcc)
                                {
                                    StringBuilder sb = new StringBuilder(500);
                                    for (int i = 0; i < actualReceive.Length; i++)
                                    {
                                        sb.Append(actualReceive[i].ToString("x2"));
                                    }
                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        this.dataShowUserCtrl.AddShowData("收到数据  (长度：" + actualReceive.Length.ToString() + ")  " + sb.ToString(), DataLevel.Default);
                                    }));

                                    int length = (actualReceive[2] << 8) + actualReceive[3];
                                    if (length < actualReceive.Length)
                                    {
                                        //原来是！=的时候进入判断，可能会造成unhandledLength为负值，导致数组越界。
                                        //处理粘包的情况。
                                        int unhandledLength = actualReceive.Length - length;
                                        byte[] packagePrevious = new byte[length];
                                        packageUnhandled = new byte[unhandledLength];
                                        for (int j = 0; j < length; j++)
                                        {
                                            packagePrevious[j] = actualReceive[j];
                                        }
                                        for (int i = 0; i < unhandledLength; i++)
                                        {
                                            packageUnhandled[i] = actualReceive[length + i];
                                        }
                                        actualReceive = new byte[length];
                                        packagePrevious.CopyTo(actualReceive, 0);
                                        goto handlePackage;
                                        //AppendMessage("长度字段与实际收到长度不相等");
                                        //continue;
                                    }
                                    int checksum = 0;
                                    for (int i = 0; i < actualReceive.Length - 2; i++)
                                    {
                                        checksum += actualReceive[i];
                                    }
                                    int sumHigh;
                                    int sumLow;
                                    sumHigh = (checksum & 0xff00) >> 8;
                                    sumLow = checksum & 0xff;
                                    if (sumHigh != actualReceive[actualReceive.Length - 2] || sumLow != actualReceive[actualReceive.Length - 1])
                                    {
                                        this.Dispatcher.Invoke(new Action(() =>
                                        {
                                            this.dataShowUserCtrl.AddShowData("校验和出错", DataLevel.Error);
                                        }));
                                        continue;
                                    }
                                }
                                else if (actualReceive[0] == 0x55 && actualReceive[1] == 0xaa)
                                {
                                    StringBuilder sb = new StringBuilder(500);
                                    for (int i = 0; i < actualReceive.Length; i++)
                                    {
                                        sb.Append(actualReceive[i].ToString("x2"));
                                    }
                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        this.dataShowUserCtrl.AddShowData("收到指令  (长度：" + actualReceive.Length.ToString() + ")  " + sb.ToString(), DataLevel.Default);
                                    }));
                                }
                                else
                                {
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                            //string strReceiveBroken = encoding.GetString(actualReceive);
                                            StringBuilder sb = new StringBuilder(500);
                                        for (int i = 0; i < actualReceive.Length; i++)
                                        {
                                            sb.Append(actualReceive[i].ToString("x2"));
                                        }
                                        this.dataShowUserCtrl.AddShowData("收到拆分的错误包  (长度：" + actualReceive.Length.ToString() + ")  " + sb.ToString(), DataLevel.Warning);
                                    }));
                                    continue;
                                }
                            }
                            catch (Exception ee)
                            {
                                AppendMessage("检查校验和异常：" + ee.Message, DataLevel.Error);
                            }
                            if (actualReceive[0] == 0x66 && actualReceive[1] == 0xcc)
                            {
                                switch (actualReceive[2])
                                {
                                    //获取配置
                                    case 0x02:
                                        {
                                            handleGetConfig(actualReceive);
                                        }
                                        break;
                                    //实时通断数据
                                    case 0xA0:
                                        {
                                            handleRealtimeAmpData(actualReceive);
                                        }
                                        break;
                                    //实时频谱信息
                                    case 0xA1:
                                        {
                                            handleRealtimeSpectrum(actualReceive);
                                        }
                                        break;
                                    default:
                                        AppendMessage("收到未知数据！", DataLevel.Error);
                                        break;
                                }
                            }
                            else
                            {
                                AppendMessage("收到的数据帧头不对！", DataLevel.Error);
                            }
                            if (packageUnhandled.Length != 0)
                            {
                                actualReceive = new byte[packageUnhandled.Length];
                                packageUnhandled.CopyTo(actualReceive, 0);
                                packageUnhandled = new byte[0];
                                goto handlePackage;
                            }
                        }
                        else
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                this.dataShowUserCtrl.AddShowData("接收数据长度小于6  " + strReceive, DataLevel.Warning);
                            }));
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (SocketException ex)
            {
                AppendMessage("Socket接收线程异常:编号" + ex.ErrorCode + "," + ex.Message, DataLevel.Error);
                switch (ex.ErrorCode)
                {
                    case 10053:
                    case 10054:
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            connectCloseHandle(_socket);
                        }));
                        break;
                    default:
                        AppendMessage("发生未处理异常！", DataLevel.Error);
                        break;
                }
            }
            catch (Exception ee)
            {
                //socketDisconnect();
                AppendMessage("Socket监听线程异常：" + ee.Message, DataLevel.Error);
            }
        }

        private void handleRealtimeSpectrum(byte[] data)
        {
            throw new NotImplementedException();
        }

        private void handleRealtimeAmpData(byte[] data)
        {
            throw new NotImplementedException();
        }

        private void handleGetConfig(byte[] data)
        {
            throw new NotImplementedException();
        }

        private int setMasterCtrlTemperature(byte tempe)
        {
            int destTempe;
            int sign = (tempe & 0x80) >> 7;
            if (sign == 1)
            {
                destTempe = -(tempe & 0x7f);
            }
            else
            {
                destTempe = tempe;
            }
            return destTempe;
        }

        private void setRail1State(int index, int onOff)
        {
            if (onOff == 0)
            {//通的
                this._svtThumbnail.Normal(new int[1] { index }, 1);
                Rail rail = this.cvsRail1.Children[index] as Rail;
                rail.Normal();
            }
            else if (onOff == 7)
            {//断的
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的1号铁轨断开！", DataLevel.Error);
                this._svtThumbnail.Error(new int[1] { index }, 1);
                Rail rail = this.cvsRail1.Children[index] as Rail;
                rail.Error();
            }
            else if (onOff == 9)
            {//超时
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的1号铁轨超时！", DataLevel.Timeout);
                this._svtThumbnail.Timeout(new int[1] { index }, 1);
                Rail rail = this.cvsRail1.Children[index] as Rail;
                rail.Timeout();
            }
            else if (onOff == 0x0a)
            {//持续干扰
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的1号铁轨持续干扰！", DataLevel.ContinuousInterference);
                this._svtThumbnail.ContinuousInterference(new int[1] { index }, 1);
                Rail rail = this.cvsRail1.Children[index] as Rail;
                rail.ContinuousInterference();
            }
            else
            {
                AppendMessage("收到未定义数据！", DataLevel.Error);
            }
        }

        private void setRail2State(int index, int onOff)
        {
            if (onOff == 0)
            {//通的
                this._svtThumbnail.Normal(new int[1] { index }, 2);
                Rail rail = this.cvsRail2.Children[index] as Rail;
                rail.Normal();
            }
            else if (onOff == 7)
            {//断的
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的2号铁轨断开！", DataLevel.Error);
                this._svtThumbnail.Error(new int[1] { index }, 2);
                Rail rail = this.cvsRail2.Children[index] as Rail;
                rail.Error();
            }
            else if (onOff == 9)
            {//超时
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的2号铁轨超时！", DataLevel.Timeout);
                this._svtThumbnail.Timeout(new int[1] { index }, 2);
                Rail rail = this.cvsRail2.Children[index] as Rail;
                rail.Timeout();
            }
            else if (onOff == 0x0a)
            {//持续干扰
                int tNo = MasterControlList[index].TerminalNumber;
                int tNextNo = MasterControlList[index + 1].TerminalNumber;

                this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的2号铁轨持续干扰！", DataLevel.ContinuousInterference);
                this._svtThumbnail.ContinuousInterference(new int[1] { index }, 2);
                Rail rail = this.cvsRail2.Children[index] as Rail;
                rail.ContinuousInterference();
            }
            else
            {
                AppendMessage("收到未定义数据！", DataLevel.Error);
            }
        }

        private void errorAllRails()
        {
            foreach (var item in _rail1List)
            {
                item.Error();
            }
            foreach (var item in cvsRail2.Children)
            {
                Rail rail2 = item as Rail;
                if (rail2 != null)
                {
                    rail2.Error();
                }
            }
        }

        private void handleBroadcastFileSize(byte[] data)
        {
            try
            {
                int size = (data[6] << 16) + (data[7] << 8) + data[8];
                long configFileSize = GetFileSize(AppDomain.CurrentDomain.BaseDirectory + "\\config.xml");
                if (configFileSize == size)
                {
                    AppendMessage("配置文件大小" + configFileSize.ToString() + "字节，与服务器相同，不需下载。", DataLevel.Warning);
                }
                else
                {
                    AppendMessage("配置文件发生改变，大小" + size.ToString() + "字节，请到菜单->设备->下载菜单项下载", DataLevel.Warning);
                }
            }
            catch (Exception ee)
            {
                AppendMessage("处理接收配置文件大小异常：" + ee.Message, DataLevel.Error);
            }
        }

        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="sFullName"></param>
        /// <returns></returns>
        public static long GetFileSize(string sFullName)
        {
            long lSize = 0;
            if (File.Exists(sFullName))
                lSize = new FileInfo(sFullName).Length;
            return lSize;
        }
        //public void SettingWinClose()
        //{
        //    this._container.Close();
        //    this._container = null;
        //}
        //private void btnSet_Click(object sender, RoutedEventArgs e)
        //{
        //    SettingUserControl newSettings = new SettingUserControl(this);
        //    if (_container == null)
        //    {
        //        _container = new Window();
        //        _container.Height = 300;
        //        _container.Width = 300;
        //        _container.Closed += container_Closed;
        //        _container.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        //        _container.Content = newSettings;
        //        _container.Show();
        //    }
        //}

        //private void container_Closed(object sender, EventArgs e)
        //{
        //    SettingWinClose();
        //}
        private void miRefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            devicesInitial();
        }
        private void miConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IPEndPoint hostIP = new IPEndPoint(IPAddress.Any, 16479);
                _socketMain = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socketMain.Bind(hostIP);
                _socketMain.Listen(500);
                _acceptSocket = _socketMain.Accept();

                _socketListeningThread = new Thread(new ParameterizedThreadStart(socketListening));
                _socketListeningThread.Start(_acceptSocket);

                _isConnect = true;
                this.miConnect.Header = "已连接";
                this.miConnect.Background = new SolidColorBrush(Colors.LightGreen);
                this.miConnect.IsEnabled = false;
            }
            catch (Exception ee)
            {
                AppendMessage("连接终端异常：" + ee.Message, DataLevel.Error);
            }
        }
        private void SendData(string data)
        {
            try
            {
                byte[] msg = Encoding.UTF8.GetBytes(data);
                _socketMain.Send(msg, 0, msg.Length, SocketFlags.None);
                this.dataShowUserCtrl.AddShowData(data, DataLevel.Default);
            }
            catch (Exception ee)
            {
                AppendMessage("发送数据异常：" + ee.Message, DataLevel.Error);
            }
        }
        private void socketAccept()
        {
            try
            {
                while (true)
                {
                    _acceptSocket = _socketMain.Accept();

                    //receiveAndAddTerminal(_acceptSocket);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.miConnect.Header = "已连接";
                        this.miConnect.Background = new SolidColorBrush(Colors.LightGreen);
                    }));

                    _socketListeningThread = new Thread(new ParameterizedThreadStart(socketListening));
                    _socketListeningThread.Start(_acceptSocket);
                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception ee)
            {
                AppendMessage("Socket接收异常：" + ee.Message, DataLevel.Error);
            }
            //finally
            //{
            //    CloseAcceptSocket();
            //}
        }
        private void receiveAndAddTerminal(Socket socket)
        {
            byte[] receivedBytes = new byte[2048];
            int numBytes = socket.Receive(receivedBytes, SocketFlags.None);
            _packageCount++;

            if (numBytes == 0)
            {
                if (_receiveEmptyPackageCount == 10)
                {
                    _receiveEmptyPackageCount = 0;
                    return;
                }
                _receiveEmptyPackageCount++;
            }
            else
            {
                _receiveEmptyPackageCount = 0;
            }
            byte[] actualReceive = new byte[numBytes];
            for (int i = 0; i < numBytes; i++)
            {
                actualReceive[i] = receivedBytes[i];
            }

            ASCIIEncoding encoding = new ASCIIEncoding();
            string strReceive = encoding.GetString(actualReceive);
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.lblPackageCount.Content = _packageCount.ToString();
                this.dataShowUserCtrl.AddShowData(socket.RemoteEndPoint.ToString() + "->  " + strReceive, DataLevel.Default);
            }));

            //检查校验和
            if (actualReceive[0] == 0x66 && actualReceive[1] == 0xcc)
            {
                int length = (actualReceive[2] << 8) + actualReceive[3];
                if (length != actualReceive.Length)
                {
                    AppendMessage("长度字段与实际收到长度不相等", DataLevel.Error);
                    return;
                }
                int checksum = 0;
                for (int i = 0; i < actualReceive.Length - 2; i++)
                {
                    checksum += actualReceive[i];
                }
                int sumHigh;
                int sumLow;
                sumHigh = (checksum & 0xff00) >> 8;
                sumLow = checksum & 0xff;
                if (sumHigh != actualReceive[actualReceive.Length - 2] || sumLow != actualReceive[actualReceive.Length - 1])
                {
                    AppendMessage("校验和出错", DataLevel.Error);
                    return;
                }
            }
        }

        public void CloseAcceptSocket()
        {
            if (_acceptSocket != null)
            {
                _acceptSocket.Disconnect(false);
                _acceptSocket.Shutdown(SocketShutdown.Both);
                _acceptSocket.Close();
            }
        }

        private void socketDisconnect()
        {
            _socketListeningThread.Abort();
            //_socketMain.Close();
            //_socketAcceptThread.Abort();
            //CloseAcceptSocket();
            //_socketMain = null;
            //this.miConnect.Header = "连接";
            //this.miConnect.Background = new SolidColorBrush((this.miCommand.Background as SolidColorBrush).Color);
        }

        private void closeSocket()
        {
            _socketMain.Close();
            //_socketAcceptThread.Abort();
            //CloseAcceptSocket();
            _socketMain = null;
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.miConnect.Header = "连接";
                this.miConnect.IsEnabled = true;
                this.miConnect.Background = new SolidColorBrush((this.miCommand.Background as SolidColorBrush).Color);
            }));
        }
        //private void miRailInitial_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        byte[] sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, 0xff, 0xf6, new byte[2] { 0, 0 });
        //        _socketMain.Send(sendData, SocketFlags.None);
        //    }
        //    catch (Exception ee)
        //    {
        //        AppendMessage(ee.Message);
        //    }
        //}

        //private void miTimeBaseCorrect_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        byte[] sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, 0xff, 0xf0, new byte[2] { 0, 0 });
        //        _socketMain.Send(sendData, SocketFlags.None);
        //        //AppendMessage(this.svContainer.ScrollableWidth.ToString() + "," + this.ActualWidth.ToString());
        //        //this.svContainer.ScrollToHorizontalOffset(9736);
        //    }
        //    catch (Exception ee)
        //    {
        //        AppendMessage(ee.Message);
        //    }
        //}

        private void miGetAllRailInfo_Click(object sender, RoutedEventArgs e)
        {
            if (_getAllRailInfoTimer.IsEnabled)
            {
                _getAllRailInfoTimer.Stop();
                //this.miGetAllRailInfo.Header = "获取所有终端铁轨信息";
            }
            else
            {
                if (!_timeToWaitTimer.IsEnabled)
                {
                    DateTime now = System.DateTime.Now;
                    int totalSecondToNow = now.Hour * 3600 + now.Minute * 60 + now.Second;
                    int timeToSend = 75 - (totalSecondToNow % 75);

                    _timeToWaitTimer.Tick += (s, ee) =>
                    {
                        _timeToWaitTimer.Stop();
                        _getAllRailInfoTimer.Start();
                        getAllRailInfoTimer_Tick(sender, e);
                        //this.miGetAllRailInfo.Header = "停止获取所有终端铁轨信息";
                    };
                    _timeToWaitTimer.Interval = new TimeSpan(0, 0, timeToSend);
                    _timeToWaitTimer.Start();
                }
            }
        }
        private void getAllRailInfoTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                this.WaitingRingEnable();
                this.WaitReceiveTimer.Start();

                if (_4GPointIndex.Count == 0)
                {
                    this.WaitingRingDisable();
                    this.WaitReceiveTimer.Stop();

                    AppendMessage("系统中不包含4G点，请检查config文档！", DataLevel.Error);
                    _getAllRailInfoTimer.Stop();
                    //this.miGetAllRailInfo.Header = "获取所有终端铁轨信息";
                }
                else
                {
                    for (int i = 0; i < _4GPointIndex.Count; i++)
                    {
                        Socket socket = this.MasterControlList[_4GPointIndex[i]].GetNearest4GTerminalSocket(true);
                        byte[] sendData;
                        if (i == _4GPointIndex.Count - 1)
                        {
                            //获取从1到ff的广播数据，当循环到最后一个的时候，目的地址不再是4G点的前一个终端，而是整个终端列表中的最后一个终端。
                            sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)this.MasterControlList[this.MasterControlList.Count - 1].TerminalNumber, (byte)CommandType.GetOneSectionInfo, new byte[2] { (byte)this.MasterControlList[_4GPointIndex[i]].TerminalNumber, 0 });
                        }
                        else
                        {
                            sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)this.MasterControlList[_4GPointIndex[i + 1] - 1].TerminalNumber, (byte)CommandType.GetOneSectionInfo, new byte[2] { (byte)this.MasterControlList[_4GPointIndex[i]].TerminalNumber, 0 });
                        }
                        if (socket != null)
                        {
                            DecideDelayOrNot();
                            socket.Send(sendData, SocketFlags.None);
                            AppendDataMsg(sendData);
                        }
                        else
                        {
                            this.WaitingRingDisable();
                            this.WaitReceiveTimer.Stop();

                            AppendMessage("来自终端" + this.MasterControlList[_4GPointIndex[i]].TerminalNumber + "的消息：" + this.MasterControlList[_4GPointIndex[i]].Find4GErrorMsg, DataLevel.Error);
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                AppendMessage(ee.Message, DataLevel.Error);
            }
        }

        private void miSubscribeAllRailInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isConnect)
                {
                    AppendMessage("请先连接！", DataLevel.Error);
                    return;
                }
                byte[] sendData;
                if (_isSubscribingAllRailInfo)
                {
                    sendData = SendDataPackage.PackageSendData((byte)this.clientIDShow.ClientID,
                            (byte)0xff, (byte)CommandType.SubscribeAllRailInfo, new byte[1] { 0xff });
                    //this.miSubscribeAllRailInfo.Header = "订阅所有终端铁轨信息";
                    errorAllRails();
                    _isSubscribingAllRailInfo = false;
                }
                else
                {
                    sendData = SendDataPackage.PackageSendData((byte)this.clientIDShow.ClientID,
                            (byte)0xff, (byte)CommandType.SubscribeAllRailInfo, new byte[1] { 0 });
                    //this.miSubscribeAllRailInfo.Header = "取消订阅所有终端铁轨信息";
                    _isSubscribingAllRailInfo = true;
                }
                if (_socketMain != null)
                {
                    _socketMain.Send(sendData, SocketFlags.None);
                    AppendDataMsg(sendData);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show("" + ee.Message);
            }
        }

        private void WaitReceiveTimer_Tick(object sender, EventArgs e)
        {
            this.WaitingRingDisable();
            this.WaitReceiveTimer.Stop();
            AppendMessage("超过20秒未收到数据，连接可能已断开！", DataLevel.Error);
        }
        private void multicastWaitReceiveTimer_Tick(object sender, EventArgs e)
        {
            this._multicastWaitReceiveTimer.Stop();
            string notReceiveNo = string.Empty;
            foreach (var item in _terminalsReceiveFlag)
            {
                if (item.Value == false)
                {
                    notReceiveNo += (item.Key + "、");
                }
            }
            if (notReceiveNo != string.Empty)
            {
                notReceiveNo = notReceiveNo.Substring(0, notReceiveNo.Length - 1);
                AppendMessage("超过20秒未收到" + notReceiveNo + "号终端的数据，终端物理链路可能已断开！", DataLevel.Error);
            }
        }
        //private void miGetAllDevicesSignalAmplitude_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        byte[] sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, 0xff, 0xf4, new byte[2] { 0, 0 });
        //        _socketMain.Send(sendData, SocketFlags.None);
        //    }
        //    catch (Exception ee)
        //    {
        //        AppendMessage(ee.Message);
        //    }
        //}

        private void miRealTimeConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime dtNow = DateTime.Now;
                int intYear = dtNow.Year;
                string strYear = intYear.ToString();
                strYear = strYear.Substring(2, 2);
                byte year = Convert.ToByte(strYear);
                byte month = (byte)dtNow.Month;
                byte day = (byte)dtNow.Day;
                byte hour = (byte)dtNow.Hour;
                byte minute = (byte)dtNow.Minute;
                byte second = (byte)dtNow.Second;
                if (_4GPointIndex.Count == 0)
                {
                    AppendMessage("系统中不包含4G点，请检查config文档！", DataLevel.Error);
                }
                else
                {
                    for (int i = 0; i < _4GPointIndex.Count; i++)
                    {
                        Socket socket = this.MasterControlList[_4GPointIndex[i]].GetNearest4GTerminalSocket(true);
                        byte[] sendData;
                        if (i == _4GPointIndex.Count - 1)
                        {
                            //获取从1到ff的广播数据，当循环到最后一个的时候，目的地址不再是4G点的前一个终端，而是整个终端列表中的最后一个终端。
                            sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)this.MasterControlList[this.MasterControlList.Count - 1].TerminalNumber, 0x52, new byte[7] { (byte)this.MasterControlList[_4GPointIndex[i]].TerminalNumber, year, month, day, hour, minute, second });
                        }
                        else
                        {
                            sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)this.MasterControlList[_4GPointIndex[i + 1] - 1].TerminalNumber, 0x52, new byte[7] { (byte)this.MasterControlList[_4GPointIndex[i]].TerminalNumber, year, month, day, hour, minute, second });
                        }
                        if (socket != null)
                        {
                            DecideDelayOrNot();
                            socket.Send(sendData, SocketFlags.None);
                            AppendDataMsg(sendData);
                        }
                        else
                        {
                            AppendMessage("来自终端" + this.MasterControlList[_4GPointIndex[i]].TerminalNumber + "的消息：" + this.MasterControlList[_4GPointIndex[i]].Find4GErrorMsg, DataLevel.Error);
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                AppendMessage(ee.Message, DataLevel.Error);
            }
        }
        private void miGetOneSectionInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GetSectionWindow newGetSectionWin = new GetSectionWindow();
                newGetSectionWin.MasterControlList = this.MasterControlList;
                newGetSectionWin.Owner = this;
                if (!newGetSectionWin.ShowDialog().Value)
                {
                    return;
                }

                this.WaitingRingEnable();
                this.WaitReceiveTimer.Start();

                List<int> include4GIndex = new List<int>();
                for (int i = 0; i < _4GPointIndex.Count; i++)
                {
                    if (this.MasterControlList[_4GPointIndex[i]].TerminalNumber >= newGetSectionWin.TerminalSmall && this.MasterControlList[_4GPointIndex[i]].TerminalNumber <= newGetSectionWin.TerminalBig)
                    {
                        include4GIndex.Add(_4GPointIndex[i]);
                    }
                }
                if (_4GPointIndex.Count == 0)
                {
                    this.WaitingRingDisable();
                    this.WaitReceiveTimer.Stop();
                    AppendMessage("系统中不包含4G点，请检查config文档！", DataLevel.Error);
                    return;
                }
                else
                {
                    if (include4GIndex.Count == 0)
                    {
                        Socket socket = this.MasterControlList[_4GPointIndex[0]].GetNearest4GTerminalSocket(true);
                        byte[] sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)newGetSectionWin.TerminalBig, 0x55, new byte[2] { (byte)newGetSectionWin.TerminalSmall, 0 });

                        if (socket != null)
                        {
                            DecideDelayOrNot();
                            socket.Send(sendData, SocketFlags.None);
                            AppendDataMsg(sendData);
                        }
                        else
                        {
                            this.WaitingRingDisable();
                            this.WaitReceiveTimer.Stop();
                            AppendMessage("来自终端" + this.MasterControlList[_4GPointIndex[0]].TerminalNumber + "的消息：" + this.MasterControlList[_4GPointIndex[0]].Find4GErrorMsg, DataLevel.Error);
                            return;
                        }
                    }
                    else
                    {
                        if (newGetSectionWin.TerminalSmall == this.MasterControlList[include4GIndex[0]].TerminalNumber)
                        {
                            for (int i = 0; i < include4GIndex.Count; i++)
                            {
                                Socket socket = this.MasterControlList[include4GIndex[i]].GetNearest4GTerminalSocket(true);
                                byte[] sendData;
                                if (i == include4GIndex.Count - 1)
                                {
                                    sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)newGetSectionWin.TerminalBig, 0x55, new byte[2] { (byte)this.MasterControlList[include4GIndex[i]].TerminalNumber, 0 });
                                }
                                else
                                {
                                    sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)this.MasterControlList[include4GIndex[i + 1] - 1].TerminalNumber, 0x55, new byte[2] { (byte)this.MasterControlList[include4GIndex[i]].TerminalNumber, 0 });
                                }
                                if (socket != null)
                                {
                                    DecideDelayOrNot();
                                    socket.Send(sendData, SocketFlags.None);
                                    AppendDataMsg(sendData);
                                }
                                else
                                {
                                    this.WaitingRingDisable();
                                    this.WaitReceiveTimer.Stop();
                                    AppendMessage("来自终端" + this.MasterControlList[include4GIndex[i]].TerminalNumber + "的消息：" + this.MasterControlList[include4GIndex[i]].Find4GErrorMsg, DataLevel.Error);
                                    return;
                                }
                            }
                        }
                        else
                        {
                            //多播段小终端号不是4G点的时候，需要去找小于小中端号的4G点。用其的Socket发送数据。
                            int previous4GPointIndex = 0;
                            for (int i = _4GPointIndex.Count - 1; i >= 0; i--)
                            {
                                if (_4GPointIndex[i] < include4GIndex[0])
                                {
                                    previous4GPointIndex = _4GPointIndex[i];
                                    break;
                                }
                            }
                            Socket socket = this.MasterControlList[previous4GPointIndex].GetNearest4GTerminalSocket(true);
                            byte[] sendData;
                            sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)this.MasterControlList[include4GIndex[0] - 1].TerminalNumber, 0x55, new byte[2] { (byte)newGetSectionWin.TerminalSmall, 0 });

                            if (socket != null)
                            {
                                DecideDelayOrNot();
                                socket.Send(sendData, SocketFlags.None);
                                AppendDataMsg(sendData);
                            }
                            else
                            {
                                this.WaitingRingDisable();
                                this.WaitReceiveTimer.Stop();
                                AppendMessage("来自终端" + this.MasterControlList[previous4GPointIndex].TerminalNumber + "的消息：" + this.MasterControlList[previous4GPointIndex].Find4GErrorMsg, DataLevel.Error);
                                return;
                            }
                            for (int i = 0; i < include4GIndex.Count; i++)
                            {
                                Socket socketAnother = this.MasterControlList[include4GIndex[i]].GetNearest4GTerminalSocket(true);
                                byte[] sendData1;
                                if (i == include4GIndex.Count - 1)
                                {
                                    sendData1 = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)newGetSectionWin.TerminalBig, 0x55, new byte[2] { (byte)this.MasterControlList[include4GIndex[i]].TerminalNumber, 0 });
                                }
                                else
                                {
                                    sendData1 = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)this.MasterControlList[include4GIndex[i + 1] - 1].TerminalNumber, 0x55, new byte[2] { (byte)this.MasterControlList[include4GIndex[i]].TerminalNumber, 0 });
                                }
                                if (socketAnother != null)
                                {
                                    socketAnother.Send(sendData1, SocketFlags.None);
                                    AppendDataMsg(sendData1);
                                }
                                else
                                {
                                    this.WaitingRingDisable();
                                    this.WaitReceiveTimer.Stop();
                                    AppendMessage("来自终端" + this.MasterControlList[include4GIndex[i]].TerminalNumber + "的消息：" + this.MasterControlList[include4GIndex[i]].Find4GErrorMsg, DataLevel.Error);
                                    return;
                                }
                            }
                        }
                    }
                }
                int terminalStartIndex = FindMasterControlIndex(newGetSectionWin.TerminalSmall);
                int terminalEndIndex = FindMasterControlIndex(newGetSectionWin.TerminalBig);
                _terminalsReceiveFlag = new Dictionary<int, bool>();
                for (int i = terminalStartIndex; i <= terminalEndIndex; i++)
                {
                    _terminalsReceiveFlag.Add(this.MasterControlList[i].TerminalNumber, false);
                }
                _multicastWaitReceiveTimer.Start();
            }
            catch (Exception ee)
            {
                AppendMessage(ee.Message, DataLevel.Error);
            }
        }

        private void miViewHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime now = System.DateTime.Now;
                string directoryName = now.ToString("yyyy") + "\\" + now.ToString("yyyy-MM");
                OpenFileDialog openFileDialog = new OpenFileDialog();
                //openFileDialog.InitialDirectory = System.Environment.CurrentDirectory + @"\History\" + directoryName;
                openFileDialog.Filter = "xml files(*.xml)|*.xml";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.FilterIndex = 1;
                if (openFileDialog.ShowDialog().Value == true)
                {
                    try
                    {
                        string fName = openFileDialog.FileName;
                        int index = fName.LastIndexOf("\\");
                        string fileNameJudge = fName.Substring(index + 1, 12);
                        string strTerminalNo = fName.Substring(index + 13, 3);
                        int terminalNo = 0;
                        if (fileNameJudge == "DataTerminal")
                        {
                            if (int.TryParse(strTerminalNo, out terminalNo))
                            {
                                RailInfoResultWindow railInfoResultWin = RailInfoResultWindow.GetInstance(terminalNo, RailInfoResultMode.获取历史模式);
                                int indexOfMaster = FindMasterControlIndex(terminalNo);
                                if (indexOfMaster == -1)
                                {
                                    AppendMessage("读取的终端号在终端集合中不存在！", DataLevel.Error);
                                    return;
                                }
                                else
                                {
                                    railInfoResultWin.MasterCtrl = this.MasterControlList[indexOfMaster];
                                }
                                railInfoResultWin.FileName = fName;
                                railInfoResultWin.RefreshResult();
                                railInfoResultWin.Show();
                            }
                            else
                            {
                                AppendMessage("终端号无法转换！更改文件名时请保留前15位！", DataLevel.Error);
                                return;
                            }
                        }
                        else
                        {
                            AppendMessage("文件名发生改变，无法获得历史数据的终端号！\r\n更改文件名时请保留前15位", DataLevel.Error);
                            return;
                        }
                    }
                    catch (Exception ee)
                    {
                        AppendMessage("文件打开异常！" + ee.Message, DataLevel.Error);
                    }
                }
            }
            catch (Exception ee)
            {
                AppendMessage("读取文件异常！" + ee.Message, DataLevel.Error);
            }
        }

        private void miExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportExcel export = new ExportExcel(this.MasterControlList);

            }
            catch (Exception ee)
            {
                AppendMessage(ee.Message, DataLevel.Error);
            }
        }

        private void miEraseFlash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EraseFlashWindow newEraseFlashWin = new EraseFlashWindow(this.MasterControlList);
                newEraseFlashWin.Owner = this;
                if (!newEraseFlashWin.ShowDialog().Value)
                {
                    return;
                }
                if (_4GPointIndex.Count == 0)
                {
                    AppendMessage("系统中不包含4G点，请检查config文档！", DataLevel.Error);
                }
                else
                {
                    //默认使用第一个4G点发送数据。多播没有分段，如果改成每个终端都是4G点需要重写逻辑。
                    Socket socket = this.MasterControlList[_4GPointIndex[0]].GetNearest4GTerminalSocket(true);

                    byte[] sendData = SendDataPackage.PackageSendData((byte)clientIDShow.ClientID, (byte)newEraseFlashWin.TerminalBig, 0x56, new byte[5] {
                    (byte)newEraseFlashWin.TerminalSmall,
                    (byte)((newEraseFlashWin.StartSectorNo & 0xff00)>>8), (byte)(newEraseFlashWin.StartSectorNo&0xff),
                    (byte)((newEraseFlashWin.EndSectorNo&0xff00)>>8), (byte)(newEraseFlashWin.EndSectorNo&0xff) });
                    if (socket != null)
                    {
                        DecideDelayOrNot();
                        socket.Send(sendData, SocketFlags.None);
                        AppendDataMsg(sendData);
                    }
                    else
                    {
                        AppendMessage("来自终端" + this.MasterControlList[_4GPointIndex[0]].TerminalNumber + "的消息：" + this.MasterControlList[_4GPointIndex[0]].Find4GErrorMsg, DataLevel.Error);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void checkDirectory()
        {
            if (!Directory.Exists(System.Environment.CurrentDirectory + @"\DataRecord"))
            {
                Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\DataRecord");
            }
            DateTime now = System.DateTime.Now;
            _directoryName = now.ToString("yyyy") + "\\" + now.ToString("yyyy-MM") + "\\" + now.ToString("yyyy-MM-dd");
            if (!Directory.Exists(System.Environment.CurrentDirectory + @"\DataRecord\" + _directoryName))
            {
                Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\DataRecord\" + _directoryName);
            }
        }
        private void initialFileConfig(int terminalNo)
        {
            string fileName = System.Environment.CurrentDirectory + @"\DataRecord\" + _directoryName + @"\DataTerminal" + terminalNo.ToString("D3") + ".xml";
            if (!File.Exists(fileName))
            {
                XmlTextWriter writer = new XmlTextWriter(fileName, null);
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("Datas");
                //writer.WriteAttributeString("Value", this.txtAimFrameNo.Text);

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
            }
        }
        private void refreshFileConfig(int terminalNo)
        {
            string fileName = System.Environment.CurrentDirectory + @"\History\" + _directoryHistoryName + @"\DataTerminal" + terminalNo.ToString("D3") + ".xml";
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            if (!Directory.Exists(System.Environment.CurrentDirectory + @"\History\" + _directoryHistoryName))
            {
                Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\History\" + _directoryHistoryName);
            }
            if (!File.Exists(fileName))
            {
                XmlTextWriter writer = new XmlTextWriter(fileName, null);
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("Datas");
                //writer.WriteAttributeString("Value", this.txtAimFrameNo.Text);

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
            }
        }

        private void checkHistoryDirectory()
        {
            if (!Directory.Exists(System.Environment.CurrentDirectory + @"\History"))
            {
                Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\History");
            }
            DateTime now = System.DateTime.Now;
            _directoryHistoryName = now.ToString("yyyy") + "\\" + now.ToString("yyyy-MM") + "\\" + now.ToString("yyyy-MM-dd");
            if (!Directory.Exists(System.Environment.CurrentDirectory + @"\History\" + _directoryHistoryName))
            {
                Directory.CreateDirectory(System.Environment.CurrentDirectory + @"\History\" + _directoryHistoryName);
            }
        }

        /// <summary>
        /// 根据终端号寻找终端所在List的索引。
        /// </summary>
        /// <param name="terminalNo">终端号</param>
        /// <returns>如果找到返回索引，否则返回-1</returns>
        public int FindMasterControlIndex(int terminalNo)
        {
            int i = 0;
            foreach (var item in this.MasterControlList)
            {
                if (item.TerminalNumber == terminalNo)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }
        public void DecideDelayOrNot()
        {
            DateTime now = System.DateTime.Now;
            int totalSecondToNow = now.Hour * 3600 + now.Minute * 60 + now.Second;
            int timeIn75Second = totalSecondToNow % 75;

            if (FindIntInSendTime(timeIn75Second))
            {
                Thread.Sleep(2000);
                this.dataShowUserCtrl.AddShowData("延时2秒发送指令！", DataLevel.Warning);
            }
        }

        public bool FindIntInSendTime(int destInt)
        {
            foreach (var item in _sendTime)
            {
                if (item == destInt)
                {
                    return true;
                }
            }
            return false;
        }

        public void WaitingRingEnable()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.modernProgressRing.IsActive = true;
                this.gridMain.IsEnabled = false;
            }));
        }

        public void WaitingRingDisable()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.modernProgressRing.IsActive = false;
                this.gridMain.IsEnabled = true;
            }));
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_socketMain != null)
            {
                _socketMain.Close();
            }
            System.Environment.Exit(0);
        }
        public void AppendDataMsg(byte[] sendData)
        {
            StringBuilder sb = new StringBuilder(500);
            for (int i = 0; i < sendData.Length; i++)
            {
                sb.Append(sendData[i].ToString("x2"));
            }
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.dataShowUserCtrl.AddShowData("发送数据  (长度：" + sendData.Length.ToString() + ")  " + sb.ToString(), DataLevel.Default);
            }));
        }

        private void miUpload_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnect)
            {
                AppendMessage("请先连接！", DataLevel.Error);
                return;
            }
            byte[] sendData;
            sendData = SendDataPackage.PackageSendData((byte)this.clientIDShow.ClientID,
                    (byte)0xff, (byte)CommandType.UploadConfig, new byte[0]);
            if (_socketMain != null)
            {
                _socketMain.Send(sendData, SocketFlags.None);
                AppendDataMsg(sendData);
            }
            else
            {
            }

            IPEndPoint deviceIP = new IPEndPoint(IPAddress.Parse(_serverIP), _fileReceivePort);
            TcpClient client = new TcpClient();
            client.Connect(deviceIP);

            AppendMessage("Start sending file...", DataLevel.Normal);
            NetworkStream stream = client.GetStream();

            //创建文件流  
            string filePath = Environment.CurrentDirectory + "//config.xml";
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            byte[] fileBuffer = new byte[1030];
            //每次传输1KB  

            int bytesRead;
            int totalBytes = 0;

            //将文件流转写入网络流  
            try
            {
                do
                {
                    //Thread.Sleep(10);//模拟远程传输视觉效果,暂停10秒  
                    bytesRead = fs.Read(fileBuffer, 0, fileBuffer.Length);
                    stream.Write(fileBuffer, 0, bytesRead);
                    totalBytes += bytesRead;
                    AppendMessage(string.Format("sending {0} bytes", bytesRead), DataLevel.Normal);
                } while (bytesRead > 0);
                AppendMessage(string.Format("Total {0} bytes sent ,Done!", totalBytes), DataLevel.Error);
            }
            catch (Exception ex)
            {
                AppendMessage(ex.Message, DataLevel.Error);
            }
            finally
            {
                stream.Dispose();
                fs.Dispose();
                client.Close();
                //listener.Stop();
            }
        }

        private void miDownload_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnect)
            {
                AppendMessage("请先连接！", DataLevel.Error);
                return;
            }
            byte[] sendData;
            sendData = SendDataPackage.PackageSendData((byte)this.clientIDShow.ClientID,
                    (byte)0xff, (byte)CommandType.RequestConfig, new byte[] { 0x48, 0x5f });
            if (_socketMain != null)
            {
                _socketMain.Send(sendData, SocketFlags.None);
                AppendDataMsg(sendData);
            }
            else
            {
            }
            Thread.Sleep(100);

            IPEndPoint deviceIP = new IPEndPoint(IPAddress.Parse(_serverIP), _fileReceivePort);
            Socket socket = new Socket(deviceIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(deviceIP);


            _socketFileRecvThread = new Thread(new ParameterizedThreadStart(socketRecvFileListening));
            _socketFileRecvThread.Start(socket);
        }

        int totalBytes = 0;
        private void socketRecvFileListening(object oSocket)
        {
            try
            {
                Socket socket = oSocket as Socket;
                if (socket != null)
                {
                    while (true)
                    {
                        byte[] receivedBytes = new byte[1024];
                        int numBytes = socket.Receive(receivedBytes, SocketFlags.None);

                        //判断Socket连接是否断开
                        if (numBytes == 0)
                        {
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                            totalBytes = 0;
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                AppendMessage("File server close.", DataLevel.Error);
                                devicesInitial();
                            }));
                            break;
                        }
                        else
                        {
                            string path = Environment.CurrentDirectory + "//config.xml";
                            if (totalBytes == 0)
                            {
                                if (File.Exists(path))
                                {
                                    File.Delete(path);
                                }
                            }

                            string msg = Encoding.UTF8.GetString(receivedBytes, 0, numBytes);

                            //从缓存Buffer中读入到文件流中  
                            FileStream fs = null;
                            if (totalBytes == 0)
                            {
                                fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                            }
                            else
                            {
                                fs = new FileStream(path, FileMode.Append, FileAccess.Write);
                            }
                            fs.Write(receivedBytes, 0, numBytes);
                            //清空缓冲区、关闭流
                            fs.Flush();
                            fs.Close();

                            totalBytes += numBytes;
                            AppendMessage(string.Format("Receiving {0}", msg), DataLevel.Default);
                            AppendMessage(string.Format("Receiving {0} bytes ...", totalBytes), DataLevel.Default);
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                AppendMessage("接收文件异常:" + ee.Message, DataLevel.Error);
            }
        }

        private void connectCloseHandle(Socket socket)
        {
            _receiveEmptyPackageCount = 0;
            AppendMessage("与服务器" + socket.RemoteEndPoint.ToString() + "的连接可能已断开！", DataLevel.Error);
            _isConnect = false;
            if (_timeToWaitTimer.IsEnabled)
            {
                _timeToWaitTimer.Stop();
            }
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.clientIDShow.ClientID = 0;
            }));
            foreach (var item in MasterControlList)
            {
                if (item.IpAndPort == socket.RemoteEndPoint.ToString())
                {
                    _socketRegister.Remove(item.TerminalNumber);
                    int index = FindMasterControlIndex(item.TerminalNumber);
                    if (index != -1)
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            MasterControlList[index].Offline();
                        }));
                    }
                    item.Dispose();
                }
            }
            try
            {
                socketDisconnect();
            }
            catch (Exception ee)
            {
                AppendMessage("关闭线程及Socket异常：" + ee.Message, DataLevel.Error);
            }
            finally
            {
                closeSocket();
                //miConnect_Click(this, null);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            devicesInitial();
        }

        private void miRealtimeInfo_Click(object sender, RoutedEventArgs e)
        {
            RealtimeInfoWindow riWin = new RealtimeInfoWindow();
            riWin.Show();
        }

        private void miGetConfigInfo_Click(object sender, RoutedEventArgs e)
        {
            byte[] sendData = SendDataPackage.PackageSendData(Command3Type.GetConfig, ConfigType.GET_CONFIG, 1, new byte[1] { 0 });
            if (_socketMain != null)
            {
                _socketMain.Send(sendData, SocketFlags.None);
                AppendDataMsg(sendData);
                AppendMessage("获取系统配置信息", DataLevel.Default);
            }
            else
            {
                AppendMessage("请先连接", DataLevel.Error);
            }
        }

        private void miSetTime_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
