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

namespace BrokenRailMonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly int MasterControlWidth = 26;
        private static readonly int RailWidth = 104;
        //private Window _container;
        private SendDataPackage _sendDataPackage = new SendDataPackage();
        private ScrollViewerThumbnail _svtThumbnail;
        private string _directoryName;
        private Socket _socketMain;
        private Socket _acceptSocket;
        private Thread _socketListeningThread;
        private Thread _socketAcceptThread;
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
            _getAllRailInfoTimer.Tick += getAllRailInfoTimer_Tick;
            _getAllRailInfoTimer.Interval = new TimeSpan(0, 0, 75);

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
                    this._rail1List.Add(rail1);
                    this._rail2List.Add(rail2);
                    this.cvsDevices.Children.Add(this.MasterControlList[this.MasterControlList.Count - 1]);
                    Canvas.SetLeft(this.MasterControlList[this.MasterControlList.Count - 1], (2 + RailWidth) * i);
                    if (i < nodeCount - 1)
                    {
                        this.cvsRail1.Children.Add(rail1);
                        Canvas.SetLeft(rail1, (2 + RailWidth) * i + MasterControlWidth / 2 + 1);

                        this.cvsRail2.Children.Add(rail2);
                        Canvas.SetLeft(rail2, (2 + RailWidth) * i + MasterControlWidth / 2 + 1);
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
                            MessageBox.Show("第一个终端的NeighbourSmall标签未设置为0");
                        }
                    }
                    else
                    {
                        if (MasterControlList[i - 1].TerminalNumber != neighbourSmall)
                        {
                            MessageBox.Show("终端" + terminalNo.ToString() + "的小相邻终端不匹配，请检查配置文件");
                        }
                        if (oneMasterControl.TerminalNumber != neighbourBigRemember)
                        {
                            MessageBox.Show("终端" + MasterControlList[i - 1].TerminalNumber.ToString() + "的大相邻终端不匹配，请检查配置文件");
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
                            MessageBox.Show("最末终端" + terminalNo.ToString() + "的大相邻终端不是ff，请检查配置文件");
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
                MessageBox.Show("设备初始化异常：" + ee.Message);
            }
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
        //        MessageBox.Show("串口早就打开了有木有!");
        //    }
        //    else
        //    {
        //        try
        //        {
        //            _serialPort1.Open();
        //            _serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);
        //            MessageBox.Show("端口打开！");
        //        }
        //        catch (Exception ee)
        //        {
        //            MessageBox.Show("端口无法打开! " + ee.Message);
        //        }
        //    }
        //}
        private void socketListening(object oSocket)
        {
            try
            {
                Socket socket = oSocket as Socket;
                if (socket != null)
                {
                    int accumulateNumber = 0;
                    while (true)
                    {
                        byte[] receivedBytes = new byte[2048];
                        int numBytes = socket.Receive(receivedBytes, SocketFlags.None);
                        //判断Socket连接是否断开
                        if (numBytes == 0)
                        {
                            if (_receiveEmptyPackageCount == 10)
                            {
                                _receiveEmptyPackageCount = 0;
                                MessageBox.Show("与" + socket.RemoteEndPoint.ToString() + "的连接可能已断开！");
                                try
                                {
                                    socketDisconnect();
                                }
                                catch (Exception ee)
                                {
                                    MessageBox.Show("关闭线程及Socket异常：" + ee.Message);
                                }
                                finally
                                {
                                    closeSocket();
                                    miConnect_Click(this, null);
                                }
                                foreach (var item in MasterControlList)
                                {
                                    if (item.IpAndPort == socket.RemoteEndPoint.ToString())
                                    {
                                        _socketRegister.Remove(item.TerminalNumber);
                                    }
                                }
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
                            bool isGreen = (this.elpIndicator.Fill as SolidColorBrush).Color.Equals(Colors.Green);
                            if (isGreen)
                            {
                                this.elpIndicator.Fill = new SolidColorBrush(Colors.White);
                            }
                            else if (isWhite)
                            {
                                this.elpIndicator.Fill = new SolidColorBrush(Colors.Green);
                            }
                        }));

                        byte[] actualReceive = new byte[numBytes];
                        for (int i = 0; i < numBytes; i++)
                        {
                            actualReceive[i] = receivedBytes[i];
                        }
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
                            numBytes = socket.Receive(receivedBytes, SocketFlags.None);
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
                            if (strReceiveFirst6Letter == "Client")
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.dataShowUserCtrl.AddShowData("收到" + socket.RemoteEndPoint.ToString() + "->  " + strReceive, DataLevel.Default);
                                }));
                                continue;
                            }
                            else if (strReceiveFirst3Letter == "###")
                            {
                                //处理心跳包
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.dataShowUserCtrl.AddShowData("收到心跳包" + socket.RemoteEndPoint.ToString() + "->  " + strReceive, DataLevel.Default);
                                }));
                                if (strReceive.Length > 5)
                                {
                                    //根据心跳包里面包含的终端号添加4G点中的socket。
                                    string strTerminalNo = strReceive.Substring(3, 3);
                                    int intTerminalNo = Convert.ToInt32(strTerminalNo);

                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        if (SocketRegister.Count == 0)
                                        {
                                            foreach (var item in MasterControlList)
                                            {
                                                if (item.TerminalNumber == intTerminalNo)
                                                {
                                                    if (!item.Is4G)
                                                    {
                                                        MessageBox.Show("心跳包中包含的终端号" + intTerminalNo.ToString() + "所示终端不是4G点，\r\n请检查心跳数据内容配置或者config文档！");
                                                        break;
                                                    }
                                                    if (item.SocketImport == null || item.IpAndPort != socket.RemoteEndPoint.ToString())
                                                    {
                                                        item.SocketImport = socket;
                                                        //socket已经导入，注册socket。
                                                        SocketRegister.Add(intTerminalNo);
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        for (int i = 0; i < SocketRegister.Count; i++)
                                        {
                                            if (SocketRegister[i] == intTerminalNo)
                                            {
                                                //找到已经注册的终端就跳出循环，不再找了，也不进行Socket赋值。
                                                break;
                                            }
                                            else if (i == SocketRegister.Count - 1)
                                            {
                                                foreach (var item in MasterControlList)
                                                {
                                                    if (item.TerminalNumber == intTerminalNo)
                                                    {
                                                        if (!item.Is4G)
                                                        {
                                                            MessageBox.Show("心跳包中包含的终端号" + intTerminalNo.ToString() + "所示终端不是4G点，\r\n请检查心跳数据内容配置或者config文档！");
                                                            break;
                                                        }
                                                        if (item.SocketImport == null || item.IpAndPort != socket.RemoteEndPoint.ToString())
                                                        {
                                                            item.SocketImport = socket;
                                                            //socket已经导入，注册socket。
                                                            SocketRegister.Add(intTerminalNo);
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }));
                                }
                                continue;
                            }
                            else
                            {
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
                                            this.dataShowUserCtrl.AddShowData("收到数据  " + sb.ToString(), DataLevel.Default);
                                        }));

                                        int length = (actualReceive[2] << 8) + actualReceive[3];
                                        if (length != actualReceive.Length)
                                        {
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
                                            //MessageBox.Show("长度字段与实际收到长度不相等");
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
                                            MessageBox.Show("校验和出错");
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        this.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            string strReceiveBroken = encoding.GetString(actualReceive);
                                            this.dataShowUserCtrl.AddShowData("收到拆分的错误包  " + strReceiveBroken, DataLevel.Warning);
                                        }));
                                        continue;
                                    }
                                }
                                catch (Exception)
                                {

                                    throw;
                                }

                            }
                            if (actualReceive[0] == 0x66 && actualReceive[1] == 0xcc)
                            {
                                switch (actualReceive[6])
                                {
                                    case 0xfe:
                                        this.Dispatcher.Invoke(new Action(() =>
                                        {
                                            switch (actualReceive[7])
                                            {
                                                case 0xf0:
                                                    this.dataShowUserCtrl.AddShowData("初始信息配置指令，4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0xf1:
                                                    this.dataShowUserCtrl.AddShowData("读取单点配置信息指令，4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0xf2:
                                                    this.dataShowUserCtrl.AddShowData("设置门限指令，4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0x52:
                                                    this.dataShowUserCtrl.AddShowData("实时时钟配置指令，4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                //case 0xf3:
                                                //    this.dataShowUserCtrl.AddShowData("超声信号发射通报指令，4G终端已接收！", DataLevel.Normal);
                                                //    break;
                                                case 0xf4:
                                                    this.dataShowUserCtrl.AddShowData("超声信号发射配置指令，4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0xf5:
                                                    this.dataShowUserCtrl.AddShowData("获取单点铁轨信息指令，4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                //case 0x56:
                                                //    this.dataShowUserCtrl.AddShowData("获取所有终端铁轨信息指令，4G终端已接收！", DataLevel.Normal);
                                                //    break;
                                                case 0x55:
                                                    this.dataShowUserCtrl.AddShowData("获取某段铁轨信息，4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                default:
                                                    this.dataShowUserCtrl.AddShowData("未知指令被接收！", DataLevel.Normal);
                                                    break;
                                            }
                                        }));
                                        break;
                                    case 0xf1:
                                        {
                                            this.Dispatcher.Invoke(new Action(() =>
                                            {
                                                this.WaitReceiveTimer.Stop();
                                                this.WaitingRingDisable();
                                            }));
                                            int terminalNo = actualReceive[7];
                                            int i = 0;
                                            int count = MasterControlList.Count;
                                            bool isError = false;
                                            this.Dispatcher.Invoke(new Action(() =>
                                            {
                                                foreach (var masterControl in MasterControlList)
                                                {
                                                    if (masterControl.TerminalNumber == terminalNo)
                                                    {
                                                        if (masterControl.NeighbourSmall != actualReceive[8])
                                                        {
                                                            MessageBox.Show(terminalNo.ToString() + "号终端相邻小终端不匹配！");
                                                            isError = true;
                                                        }
                                                        if (masterControl.NeighbourBig != actualReceive[9])
                                                        {
                                                            MessageBox.Show(terminalNo.ToString() + "号终端相邻大终端不匹配！");
                                                            isError = true;
                                                        }
                                                        if (actualReceive[10] != (masterControl.Is4G ? 1 : 0))
                                                        {
                                                            MessageBox.Show(terminalNo.ToString() + "号终端Is4G不匹配！");
                                                            isError = true;
                                                        }
                                                        if (actualReceive[11] != (masterControl.IsEnd ? 1 : 0))
                                                        {
                                                            MessageBox.Show(terminalNo.ToString() + "号终端IsEnd不匹配！");
                                                            isError = true;
                                                        }
                                                        if (!isError)
                                                        {
                                                            PointInfoResultWindow onePIRWin = new PointInfoResultWindow(masterControl.TerminalNumber,
                                                                masterControl.NeighbourSmall, masterControl.NeighbourBig,
                                                                masterControl.Is4G, masterControl.IsEnd);
                                                            onePIRWin.Owner = this;
                                                            onePIRWin.ShowDialog();
                                                        }
                                                        break;
                                                    }
                                                    if (count - 1 == i)
                                                    {
                                                        MessageBox.Show(terminalNo.ToString() + "号终端不存在");
                                                    }
                                                    i++;
                                                }
                                            }));
                                        }
                                        break;
                                    case 0xf5:
                                        {
                                            this.Dispatcher.Invoke(new Action(() =>
                                            {
                                                this.WaitReceiveTimer.Stop();
                                                this.WaitingRingDisable();
                                            }));
                                            checkDirectory();
                                            initialFileConfig(actualReceive[7]);
                                            string fileName = System.Environment.CurrentDirectory + @"\DataRecord\" + _directoryName + @"\DataTerminal" + actualReceive[7].ToString("D3") + ".xml";
                                            if (File.Exists(fileName))
                                            {
                                                XmlDocument xmlDoc = new XmlDocument();
                                                xmlDoc.Load(fileName);
                                                XmlNode xn1 = xmlDoc.SelectSingleNode("Datas");
                                                if (xn1 != null)
                                                {
                                                    XmlElement xeData = xmlDoc.CreateElement("Data");//创建一个<Data>节点
                                                    xn1.AppendChild(xeData);
                                                    XmlElement xeTime = xmlDoc.CreateElement("Time");
                                                    xeData.AppendChild(xeTime);
                                                    XmlElement xeRail1 = xmlDoc.CreateElement("Rail1");
                                                    xeData.AppendChild(xeRail1);
                                                    XmlElement xeOnOff1 = xmlDoc.CreateElement("OnOff");
                                                    xeRail1.AppendChild(xeOnOff1);
                                                    XmlElement xeStress1 = xmlDoc.CreateElement("Stress");
                                                    xeRail1.AppendChild(xeStress1);
                                                    XmlElement xeTemprature1 = xmlDoc.CreateElement("Temprature");
                                                    xeRail1.AppendChild(xeTemprature1);
                                                    XmlElement xeThisAmplitude1 = xmlDoc.CreateElement("ThisAmplitude");
                                                    xeRail1.AppendChild(xeThisAmplitude1);
                                                    XmlElement xeSignalAmplitude1Left = xmlDoc.CreateElement("SignalAmplitudeLeft");
                                                    xeRail1.AppendChild(xeSignalAmplitude1Left);
                                                    XmlElement xeSignalAmplitude1Right = xmlDoc.CreateElement("SignalAmplitudeRight");
                                                    xeRail1.AppendChild(xeSignalAmplitude1Right);
                                                    XmlElement xeRail2 = xmlDoc.CreateElement("Rail2");
                                                    xeData.AppendChild(xeRail2);
                                                    XmlElement xeOnOff2 = xmlDoc.CreateElement("OnOff");
                                                    xeRail2.AppendChild(xeOnOff2);
                                                    XmlElement xeStress2 = xmlDoc.CreateElement("Stress");
                                                    xeRail2.AppendChild(xeStress2);
                                                    XmlElement xeTemprature2 = xmlDoc.CreateElement("Temprature");
                                                    xeRail2.AppendChild(xeTemprature2);
                                                    XmlElement xeThisAmplitude2 = xmlDoc.CreateElement("ThisAmplitude");
                                                    xeRail2.AppendChild(xeThisAmplitude2);
                                                    XmlElement xeSignalAmplitude2Left = xmlDoc.CreateElement("SignalAmplitudeLeft");
                                                    xeRail2.AppendChild(xeSignalAmplitude2Left);
                                                    XmlElement xeSignalAmplitude2Right = xmlDoc.CreateElement("SignalAmplitudeRight");
                                                    xeRail2.AppendChild(xeSignalAmplitude2Right);
                                                    xeTime.InnerText = actualReceive[8].ToString() + "-" + actualReceive[9].ToString() + "-" +
                                                                       actualReceive[10].ToString() + "-" + actualReceive[11].ToString() + "-" +
                                                                       actualReceive[12].ToString() + "-" + actualReceive[13].ToString();
                                                    xeOnOff1.InnerText = actualReceive[14].ToString();
                                                    xeStress1.InnerText = actualReceive[15].ToString() + "-" + actualReceive[16].ToString();
                                                    xeTemprature1.InnerText = actualReceive[17].ToString() + "-" + actualReceive[18].ToString();
                                                    xeThisAmplitude1.InnerText = actualReceive[19].ToString() + "-" + actualReceive[20].ToString();
                                                    string strSignalAmplitude = "";
                                                    for (int i = 21; i < 37; i++)
                                                    {
                                                        strSignalAmplitude += actualReceive[i].ToString();
                                                        if (i == 36)
                                                        {
                                                            continue;
                                                        }
                                                        strSignalAmplitude += "-";
                                                    }
                                                    xeSignalAmplitude1Left.InnerText = strSignalAmplitude;
                                                    strSignalAmplitude = "";
                                                    for (int i = 37; i < 53; i++)
                                                    {
                                                        strSignalAmplitude += actualReceive[i].ToString();
                                                        if (i == 52)
                                                        {
                                                            continue;
                                                        }
                                                        strSignalAmplitude += "-";
                                                    }
                                                    xeSignalAmplitude1Right.InnerText = strSignalAmplitude;
                                                    xeOnOff2.InnerText = actualReceive[53].ToString();
                                                    xeStress2.InnerText = actualReceive[54].ToString() + "-" + actualReceive[55].ToString();
                                                    xeTemprature2.InnerText = actualReceive[56].ToString() + "-" + actualReceive[57].ToString();
                                                    if (actualReceive[56] == 1)
                                                    {
                                                        this.Dispatcher.Invoke(new Action(() =>
                                                        {
                                                            this.dataShowUserCtrl.AddShowData(actualReceive[7].ToString() + "号终端收到非相邻终端发来的超声信号！", DataLevel.Warning);
                                                        }));
                                                    }
                                                    else if (actualReceive[56] == 0)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        MessageBox.Show("'收到非相邻终端发来的超声信号'位收到未定义数据！");
                                                    }
                                                    xeThisAmplitude2.InnerText = actualReceive[58].ToString() + "-" + actualReceive[59].ToString();
                                                    strSignalAmplitude = "";
                                                    for (int i = 60; i < 76; i++)
                                                    {
                                                        strSignalAmplitude += actualReceive[i].ToString();
                                                        if (i == 75)
                                                        {
                                                            continue;
                                                        }
                                                        strSignalAmplitude += "-";
                                                    }
                                                    xeSignalAmplitude2Left.InnerText = strSignalAmplitude;
                                                    strSignalAmplitude = "";
                                                    for (int i = 76; i < 92; i++)
                                                    {
                                                        strSignalAmplitude += actualReceive[i].ToString();
                                                        if (i == 91)
                                                        {
                                                            continue;
                                                        }
                                                        strSignalAmplitude += "-";
                                                    }
                                                    xeSignalAmplitude2Right.InnerText = strSignalAmplitude;
                                                    //xe1.SetAttribute("Value", this.txtAimFrameNo.Text);设置该节点Value属性
                                                    xmlDoc.Save(fileName);
                                                }
                                            }
                                            this.Dispatcher.Invoke(new Action(() =>
                                            {
                                                RailInfoResultWindow railInfoResultWin = RailInfoResultWindow.GetInstance(actualReceive[7]);
                                                int index = findMasterControlIndex(actualReceive[7]);
                                                if (index == -1)
                                                {
                                                    MessageBox.Show("收到的终端号在终端集合中不存在！");
                                                    return;
                                                }
                                                else
                                                {
                                                    railInfoResultWin.MasterCtrl = this.MasterControlList[index];
                                                }
                                                railInfoResultWin.RefreshResult();
                                                railInfoResultWin.Show();
                                            }));
                                        }
                                        break;
                                    case 0x55:
                                    case 0x56:
                                        {
                                            if (this._svtThumbnail == null)
                                            {
                                                MessageBox.Show("设备及铁轨未初始化！");
                                                return;
                                            }

                                            this.WaitingRingDisable();
                                            this.WaitReceiveTimer.Stop();

                                            int length = (actualReceive[2] << 8) + actualReceive[3];
                                            byte[] bytesOnOffContent = new byte[length - 9];
                                            byte[] bytesTemp = new byte[length - 9];
                                            for (int i = 7; i < length - 2; i++)
                                            {
                                                bytesOnOffContent[i - 7] = actualReceive[i];
                                            }
                                            for (int i = 0; i < bytesOnOffContent.Length; i += 3)
                                            {
                                                bytesTemp[i] = bytesOnOffContent[bytesOnOffContent.Length - i - 3];
                                                bytesTemp[i + 1] = bytesOnOffContent[bytesOnOffContent.Length - i - 2];
                                                bytesTemp[i + 2] = bytesOnOffContent[bytesOnOffContent.Length - i - 1];
                                            }
                                            bytesTemp.CopyTo(bytesOnOffContent, 0);
                                            int contentLength = bytesOnOffContent.Length;
                                            if (contentLength % 3 == 0)
                                            {
                                                if (contentLength == 3)
                                                {
                                                    //如果只有一个终端的数据就不存在两个终端数据冲突的情况。
                                                    int index = findMasterControlIndex(bytesOnOffContent[0]);
                                                    if (_terminalsReceiveFlag != null)
                                                    {
                                                        _terminalsReceiveFlag[bytesOnOffContent[0]] = true;
                                                    }
                                                    //检查1号铁轨
                                                    if (index != 0)
                                                    {
                                                        //第一个终端没有左边的铁轨
                                                        int onOffRail1Left = bytesOnOffContent[1] & 0x0f;
                                                        this.Dispatcher.Invoke(new Action(() =>
                                                        {
                                                            setRail1State(index - 1, onOffRail1Left);
                                                        }));
                                                    }
                                                    if (index != MasterControlList.Count - 1)
                                                    {
                                                        //最后一个终端没有右边的铁轨
                                                        int onOffRail1Right = (bytesOnOffContent[1] & 0xf0) >> 4;
                                                        this.Dispatcher.Invoke(new Action(() =>
                                                        {
                                                            setRail1State(index, onOffRail1Right);
                                                        }));
                                                    }

                                                    //检查2号铁轨
                                                    if (index != 0)
                                                    {
                                                        //第一个终端没有左边的铁轨
                                                        int onOffRail2Left = bytesOnOffContent[2] & 0x0f;
                                                        this.Dispatcher.Invoke(new Action(() =>
                                                        {
                                                            setRail2State(index - 1, onOffRail2Left);
                                                        }));
                                                    }
                                                    if (index != MasterControlList.Count - 1)
                                                    {
                                                        //最后一个终端没有右边的铁轨
                                                        int onOffRail2Right = (bytesOnOffContent[2] & 0xf0) >> 4;
                                                        this.Dispatcher.Invoke(new Action(() =>
                                                        {
                                                            setRail2State(index, onOffRail2Right);
                                                        }));
                                                    }
                                                }
                                                else
                                                {
                                                    //如果有多个终端的数据，需要处理冲突。
                                                    for (int i = 0; i < contentLength - 3; i++, i++, i++)
                                                    {
                                                        int index = findMasterControlIndex(bytesOnOffContent[i]);
                                                        if (_terminalsReceiveFlag != null)
                                                        {
                                                            _terminalsReceiveFlag[bytesOnOffContent[i]] = true;
                                                        }
                                                        //检查1号铁轨
                                                        if (i == 0 && index != 0)
                                                        {
                                                            //第一个终端没有左边的铁轨
                                                            int onOffRail1Left = bytesOnOffContent[1] & 0x0f;
                                                            this.Dispatcher.Invoke(new Action(() =>
                                                            {
                                                                setRail1State(index - 1, onOffRail1Left);
                                                            }));
                                                        }
                                                        else
                                                        {
                                                            if (((bytesOnOffContent[i + 1] & 0xf0) >> 4) == (bytesOnOffContent[i + 4] & 0x0f))
                                                            {
                                                                //不冲突
                                                                int onOff = (bytesOnOffContent[i + 1] & 0xf0) >> 4;
                                                                this.Dispatcher.Invoke(new Action(() =>
                                                                {
                                                                    setRail1State(index, onOff);
                                                                }));
                                                            }
                                                            else
                                                            {
                                                                //冲突
                                                                this.Dispatcher.Invoke(new Action(() =>
                                                                {
                                                                    this._svtThumbnail.Different(new int[1] { index }, 1);
                                                                    Rail rail = this.cvsRail1.Children[index] as Rail;
                                                                    rail.Different();

                                                                    int tNo = MasterControlList[index].TerminalNumber;
                                                                    int tNextNo = MasterControlList[index + 1].TerminalNumber;
                                                                    string errorTerminal = string.Empty;
                                                                    if ((bytesOnOffContent[i + 1] & 0xf0) == 0x70)
                                                                    {
                                                                        errorTerminal = tNo.ToString() + "号终端接收异常";
                                                                    }
                                                                    else if ((bytesOnOffContent[i + 4] & 0x0f) == 0x07)
                                                                    {
                                                                        errorTerminal = tNextNo.ToString() + "号终端接收异常";
                                                                    }
                                                                    this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的1号铁轨通断信息矛盾！" + errorTerminal +
                                                                        "，请检查", DataLevel.Warning);
                                                                }));
                                                            }
                                                        }
                                                        if (i == (contentLength - 6))
                                                        {
                                                            int indexLastTerminal = findMasterControlIndex(bytesOnOffContent[i + 3]);
                                                            if (_terminalsReceiveFlag != null)
                                                            {
                                                                _terminalsReceiveFlag[bytesOnOffContent[i + 3]] = true;
                                                            }
                                                            if (indexLastTerminal != MasterControlList.Count - 1)
                                                            {
                                                                //最后一个终端没有右边的铁轨
                                                                int onOffRail1Right = (bytesOnOffContent[i + 4] & 0xf0) >> 4;
                                                                this.Dispatcher.Invoke(new Action(() =>
                                                                {
                                                                    setRail1State(indexLastTerminal, onOffRail1Right);
                                                                }));
                                                            }
                                                        }

                                                        //检查2号铁轨
                                                        if (i == 0 && index != 0)
                                                        {
                                                            //第一个终端没有左边的铁轨
                                                            int onOffRail2Left = bytesOnOffContent[2] & 0x0f;
                                                            this.Dispatcher.Invoke(new Action(() =>
                                                            {
                                                                setRail2State(index - 1, onOffRail2Left);
                                                            }));
                                                        }
                                                        else
                                                        {
                                                            if (((bytesOnOffContent[i + 2] & 0xf0) >> 4) == (bytesOnOffContent[i + 5] & 0x0f))
                                                            {
                                                                //不冲突
                                                                int onOff = (bytesOnOffContent[i + 2] & 0xf0) >> 4;
                                                                this.Dispatcher.Invoke(new Action(() =>
                                                                {
                                                                    setRail2State(index, onOff);
                                                                }));
                                                            }
                                                            else
                                                            {
                                                                //冲突
                                                                this.Dispatcher.Invoke(new Action(() =>
                                                                {
                                                                    this._svtThumbnail.Different(new int[1] { index }, 2);
                                                                    Rail rail = this.cvsRail2.Children[index] as Rail;
                                                                    rail.Different();

                                                                    int tNo = MasterControlList[index].TerminalNumber;
                                                                    int tNextNo = MasterControlList[index + 1].TerminalNumber;
                                                                    string errorTerminal = string.Empty;
                                                                    if ((bytesOnOffContent[i + 2] & 0xf0) == 0x70)
                                                                    {
                                                                        errorTerminal = tNo.ToString() + "号终端接收异常";
                                                                    }
                                                                    else if ((bytesOnOffContent[i + 5] & 0x0f) == 0x07)
                                                                    {
                                                                        errorTerminal = tNextNo.ToString() + "号终端接收异常";
                                                                    }
                                                                    this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的2号铁轨通断信息矛盾！" + errorTerminal +
                                                                        "，请检查", DataLevel.Warning);
                                                                }));
                                                            }
                                                        }
                                                        if (i == (contentLength - 6))
                                                        {
                                                            int indexLastTerminal = findMasterControlIndex(bytesOnOffContent[i + 3]);
                                                            if (indexLastTerminal != MasterControlList.Count - 1)
                                                            {
                                                                //最后一个终端没有右边的铁轨
                                                                int onOffRail2Right = (bytesOnOffContent[i + 5] & 0xf0) >> 4;
                                                                this.Dispatcher.Invoke(new Action(() =>
                                                                {
                                                                    setRail2State(indexLastTerminal, onOffRail2Right);
                                                                }));
                                                            }
                                                        }
                                                    }
                                                }

                                                int rail1NormalCount = 0;
                                                int rail2NormalCount = 0;
                                                this.Dispatcher.Invoke(new Action(() =>
                                                {
                                                    for (int i = 0; i < this.cvsRail1.Children.Count; i++)
                                                    {
                                                        var rail1 = this.cvsRail1.Children[i] as Rail;
                                                        if (rail1.RailState == RailStates.IsNormal)
                                                        {
                                                            rail1NormalCount++;
                                                        }
                                                        if (rail1NormalCount == this.cvsRail1.Children.Count)
                                                        {
                                                            this.dataShowUserCtrl.AddShowData("1号铁轨正常", DataLevel.Normal);
                                                        }
                                                        var rail2 = this.cvsRail2.Children[i] as Rail;
                                                        if (rail2.RailState == RailStates.IsNormal)
                                                        {
                                                            rail2NormalCount++;
                                                        }
                                                        if (rail2NormalCount == this.cvsRail2.Children.Count)
                                                        {
                                                            this.dataShowUserCtrl.AddShowData("2号铁轨正常", DataLevel.Normal);
                                                        }
                                                    }
                                                }));
                                            }
                                            else
                                            {
                                                MessageBox.Show("发送数据内容的长度错误，应该是3的倍数");
                                            }
                                        }
                                        break;
                                    case 0x88:
                                        {
                                            this.WaitingRingDisable();
                                            this.WaitReceiveTimer.Stop();
                                            this._multicastWaitReceiveTimer.Stop();

                                            this.Dispatcher.BeginInvoke(new Action(() =>
                                            {
                                                this.dataShowUserCtrl.AddShowData(actualReceive[7].ToString() + "号终端失联，未收到其返回的数据！", DataLevel.Error);
                                            }));
                                        }
                                        break;
                                    //case 0xf7:
                                    //    {
                                    //        int errorTerminalNo = actualReceive[7];
                                    //        MessageBox.Show(errorTerminalNo.ToString() + " 号终端回应超时！");
                                    //    }
                                    //    break;
                                    default:
                                        MessageBox.Show("收到未知数据！");
                                        break;
                                }
                            }
                            else
                            {
                                MessageBox.Show("收到的数据帧头不对！");
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
                            this.Dispatcher.BeginInvoke(new Action(() =>
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
            catch (Exception ee)
            {
                //socketDisconnect();
                MessageBox.Show("Socket监听线程异常：" + ee.Message);
            }
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
            else
            {
                MessageBox.Show("收到未定义数据！");
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
            else
            {
                MessageBox.Show("收到未定义数据！");
            }
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
                //IPEndPoint deviceIP = new IPEndPoint(IPAddress.Parse("192.168.16.254"), 8080);
                //IPEndPoint deviceIP = new IPEndPoint(IPAddress.Parse("103.44.145.248"), 13317);
                //_socketMain = new Socket(deviceIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //_socketMain.Connect(deviceIP);
                //_socketListeningThread = new Thread(socketListening);
                //_socketListeningThread.Start();
                IPEndPoint hostIP = new IPEndPoint(IPAddress.Any, 16479);
                _socketMain = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socketMain.Bind(hostIP);
                _socketMain.Listen(500);
                _socketAcceptThread = new Thread(socketAccept);
                _socketAcceptThread.IsBackground = true;
                _socketAcceptThread.Start();
            }
            catch (Exception ee)
            {
                MessageBox.Show("连接终端异常：" + ee.Message);
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
                MessageBox.Show("Socket接收异常：" + ee.Message);
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
                    MessageBox.Show("长度字段与实际收到长度不相等");
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
                    MessageBox.Show("校验和出错");
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
            _socketAcceptThread.Abort();
            CloseAcceptSocket();
            _socketMain = null;
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.miConnect.Header = "连接";
                this.miConnect.Background = new SolidColorBrush((this.miCommand.Background as SolidColorBrush).Color);
            }));
        }
        //private void miRailInitial_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        byte[] sendData = _sendDataPackage.PackageSendData(0xff, 0xff, 0xf6, new byte[2] { 0, 0 });
        //        _socketMain.Send(sendData, SocketFlags.None);
        //    }
        //    catch (Exception ee)
        //    {
        //        MessageBox.Show(ee.Message);
        //    }
        //}

        //private void miTimeBaseCorrect_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        byte[] sendData = _sendDataPackage.PackageSendData(0xff, 0xff, 0xf0, new byte[2] { 0, 0 });
        //        _socketMain.Send(sendData, SocketFlags.None);
        //        //MessageBox.Show(this.svContainer.ScrollableWidth.ToString() + "," + this.ActualWidth.ToString());
        //        //this.svContainer.ScrollToHorizontalOffset(9736);
        //    }
        //    catch (Exception ee)
        //    {
        //        MessageBox.Show(ee.Message);
        //    }
        //}

        private void miGetAllRailInfo_Click(object sender, RoutedEventArgs e)
        {
            if (_getAllRailInfoTimer.IsEnabled)
            {
                _getAllRailInfoTimer.Stop();
                this.miGetAllRailInfo.Header = "获取所有终端铁轨信息";
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
                        this.miGetAllRailInfo.Header = "停止获取所有终端铁轨信息";
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

                    MessageBox.Show("系统中不包含4G点，请检查config文档！");
                    _getAllRailInfoTimer.Stop();
                    this.miGetAllRailInfo.Header = "获取所有终端铁轨信息";
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
                            sendData = _sendDataPackage.PackageSendData(0xff, (byte)this.MasterControlList[this.MasterControlList.Count - 1].TerminalNumber, 0x55, new byte[2] { (byte)this.MasterControlList[_4GPointIndex[i]].TerminalNumber, 0 });
                        }
                        else
                        {
                            sendData = _sendDataPackage.PackageSendData(0xff, (byte)this.MasterControlList[_4GPointIndex[i + 1] - 1].TerminalNumber, 0x55, new byte[2] { (byte)this.MasterControlList[_4GPointIndex[i]].TerminalNumber, 0 });
                        }
                        if (socket != null)
                        {
                            DecideDelayOrNot();
                            socket.Send(sendData, SocketFlags.None);
                        }
                        else
                        {
                            this.WaitingRingDisable();
                            this.WaitReceiveTimer.Stop();

                            MessageBox.Show(this.MasterControlList[_4GPointIndex[i]].Find4GErrorMsg, "来自终端" + this.MasterControlList[_4GPointIndex[i]].TerminalNumber + "的消息：");
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void WaitReceiveTimer_Tick(object sender, EventArgs e)
        {
            this.WaitingRingDisable();
            this.WaitReceiveTimer.Stop();
            MessageBox.Show("超过20秒未收到数据，连接可能已断开！");
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
                MessageBox.Show("超过20秒未收到" + notReceiveNo + "号终端的数据，终端物理链路可能已断开！");
            }
        }
        //private void miGetAllDevicesSignalAmplitude_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        byte[] sendData = _sendDataPackage.PackageSendData(0xff, 0xff, 0xf4, new byte[2] { 0, 0 });
        //        _socketMain.Send(sendData, SocketFlags.None);
        //    }
        //    catch (Exception ee)
        //    {
        //        MessageBox.Show(ee.Message);
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
                    MessageBox.Show("系统中不包含4G点，请检查config文档！");
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
                            sendData = _sendDataPackage.PackageSendData(0xff, (byte)this.MasterControlList[this.MasterControlList.Count - 1].TerminalNumber, 0x52, new byte[7] { (byte)this.MasterControlList[_4GPointIndex[i]].TerminalNumber, year, month, day, hour, minute, second });
                        }
                        else
                        {
                            sendData = _sendDataPackage.PackageSendData(0xff, (byte)this.MasterControlList[_4GPointIndex[i + 1] - 1].TerminalNumber, 0x52, new byte[7] { (byte)this.MasterControlList[_4GPointIndex[i]].TerminalNumber, year, month, day, hour, minute, second });
                        }
                        if (socket != null)
                        {
                            DecideDelayOrNot();
                            socket.Send(sendData, SocketFlags.None);
                        }
                        else
                        {
                            MessageBox.Show(this.MasterControlList[_4GPointIndex[i]].Find4GErrorMsg, "来自终端" + this.MasterControlList[_4GPointIndex[i]].TerminalNumber + "的消息：");
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
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
                    MessageBox.Show("系统中不包含4G点，请检查config文档！");
                    return;
                }
                else
                {
                    if (include4GIndex.Count == 0)
                    {
                        Socket socket = this.MasterControlList[_4GPointIndex[0]].GetNearest4GTerminalSocket(true);
                        byte[] sendData = _sendDataPackage.PackageSendData(0xff, (byte)newGetSectionWin.TerminalBig, 0x55, new byte[2] { (byte)newGetSectionWin.TerminalSmall, 0 });

                        if (socket != null)
                        {
                            DecideDelayOrNot();
                            socket.Send(sendData, SocketFlags.None);
                        }
                        else
                        {
                            this.WaitingRingDisable();
                            this.WaitReceiveTimer.Stop();
                            MessageBox.Show(this.MasterControlList[_4GPointIndex[0]].Find4GErrorMsg, "来自终端" + this.MasterControlList[_4GPointIndex[0]].TerminalNumber + "的消息：");
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
                                    sendData = _sendDataPackage.PackageSendData(0xff, (byte)newGetSectionWin.TerminalBig, 0x55, new byte[2] { (byte)this.MasterControlList[include4GIndex[i]].TerminalNumber, 0 });
                                }
                                else
                                {
                                    sendData = _sendDataPackage.PackageSendData(0xff, (byte)this.MasterControlList[include4GIndex[i + 1] - 1].TerminalNumber, 0x55, new byte[2] { (byte)this.MasterControlList[include4GIndex[i]].TerminalNumber, 0 });
                                }
                                if (socket != null)
                                {
                                    DecideDelayOrNot();
                                    socket.Send(sendData, SocketFlags.None);
                                }
                                else
                                {
                                    this.WaitingRingDisable();
                                    this.WaitReceiveTimer.Stop();
                                    MessageBox.Show(this.MasterControlList[include4GIndex[i]].Find4GErrorMsg, "来自终端" + this.MasterControlList[include4GIndex[i]].TerminalNumber + "的消息：");
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
                            sendData = _sendDataPackage.PackageSendData(0xff, (byte)this.MasterControlList[include4GIndex[0] - 1].TerminalNumber, 0x55, new byte[2] { (byte)newGetSectionWin.TerminalSmall, 0 });

                            if (socket != null)
                            {
                                DecideDelayOrNot();
                                socket.Send(sendData, SocketFlags.None);
                            }
                            else
                            {
                                this.WaitingRingDisable();
                                this.WaitReceiveTimer.Stop();
                                MessageBox.Show(this.MasterControlList[previous4GPointIndex].Find4GErrorMsg, "来自终端" + this.MasterControlList[previous4GPointIndex].TerminalNumber + "的消息：");
                                return;
                            }
                            for (int i = 0; i < include4GIndex.Count; i++)
                            {
                                Socket socketAnother = this.MasterControlList[include4GIndex[i]].GetNearest4GTerminalSocket(true);
                                byte[] sendData1;
                                if (i == include4GIndex.Count - 1)
                                {
                                    sendData1 = _sendDataPackage.PackageSendData(0xff, (byte)newGetSectionWin.TerminalBig, 0x55, new byte[2] { (byte)this.MasterControlList[include4GIndex[i]].TerminalNumber, 0 });
                                }
                                else
                                {
                                    sendData1 = _sendDataPackage.PackageSendData(0xff, (byte)this.MasterControlList[include4GIndex[i + 1] - 1].TerminalNumber, 0x55, new byte[2] { (byte)this.MasterControlList[include4GIndex[i]].TerminalNumber, 0 });
                                }
                                if (socketAnother != null)
                                {
                                    socketAnother.Send(sendData1, SocketFlags.None);
                                }
                                else
                                {
                                    this.WaitingRingDisable();
                                    this.WaitReceiveTimer.Stop();
                                    MessageBox.Show(this.MasterControlList[include4GIndex[i]].Find4GErrorMsg, "来自终端" + this.MasterControlList[include4GIndex[i]].TerminalNumber + "的消息：");
                                    return;
                                }
                            }
                        }
                    }
                }
                int terminalStartIndex = findMasterControlIndex(newGetSectionWin.TerminalSmall);
                int terminalEndIndex = findMasterControlIndex(newGetSectionWin.TerminalBig);
                _terminalsReceiveFlag = new Dictionary<int, bool>();
                for (int i = terminalStartIndex; i <= terminalEndIndex; i++)
                {
                    _terminalsReceiveFlag.Add(this.MasterControlList[i].TerminalNumber, false);
                }
                _multicastWaitReceiveTimer.Start();
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
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
        /// <summary>
        /// 根据终端号寻找终端所在List的索引。
        /// </summary>
        /// <param name="terminalNo">终端号</param>
        /// <returns>如果找到返回索引，否则返回-1</returns>
        private int findMasterControlIndex(int terminalNo)
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
            System.Environment.Exit(0);
        }
    }
}
