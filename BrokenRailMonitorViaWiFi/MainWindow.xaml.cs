﻿using BrokenRailMonitorViaWiFi.Windows;
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

namespace BrokenRailMonitorViaWiFi
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
        private const String _serverWeb = "f1880f0253.51mypc.cn";
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
                        byte[] receivedBytes = new byte[4096];
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
                            if (strReceiveFirst6Letter == "Client")
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.dataShowUserCtrl.AddShowData("收到" + _socket.RemoteEndPoint.ToString() + "->  " + strReceive, DataLevel.Default);
                                }));
                                continue;
                            }
                            else if (strReceiveFirst3Letter == "###")
                            {
                                //处理心跳包
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.dataShowUserCtrl.AddShowData("收到心跳包" + _socket.RemoteEndPoint.ToString() + "->  " + strReceive, DataLevel.Default);
                                }));
                                if (strReceive.Length > 5)
                                {
                                    //根据心跳包里面包含的终端号添加4G点中的socket。
                                    string strTerminalNo = strReceive.Substring(3, 3);
                                    int intTerminalNo = Convert.ToInt32(strTerminalNo);

                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        int index = FindMasterControlIndex(intTerminalNo);
                                        if (index != -1)
                                            MasterControlList[index].Online();
                                        if (SocketRegister.Count == 0)
                                        {
                                            foreach (var item in MasterControlList)
                                            {
                                                if (item.TerminalNumber == intTerminalNo)
                                                {
                                                    if (!item.Is4G)
                                                    {
                                                        AppendMessage("心跳包中包含的终端号" + intTerminalNo.ToString() + "所示终端不是4G点，\r\n请检查心跳数据内容配置或者config文档！", DataLevel.Error);
                                                        break;
                                                    }
                                                    if (item.SocketImport == null || item.IpAndPort != _socket.RemoteEndPoint.ToString())
                                                    {
                                                        item.SocketImport = _socket;
                                                        //socket已经导入，注册socket。
                                                        SocketRegister.Add(intTerminalNo);
                                                        this.Dispatcher.Invoke(new Action(() =>
                                                        {
                                                            this.dataShowUserCtrl.AddShowData(intTerminalNo.ToString() + "号终端4G点Socket注册", DataLevel.Normal);
                                                        }));
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
                                                            AppendMessage("心跳包中包含的终端号" + intTerminalNo.ToString() + "所示终端不是4G点，\r\n请检查心跳数据内容配置或者config文档！", DataLevel.Error);
                                                            break;
                                                        }
                                                        if (item.SocketImport == null || item.IpAndPort != _socket.RemoteEndPoint.ToString())
                                                        {
                                                            item.SocketImport = _socket;
                                                            //socket已经导入，注册socket。
                                                            SocketRegister.Add(intTerminalNo);
                                                            this.Dispatcher.Invoke(new Action(() =>
                                                            {
                                                                this.dataShowUserCtrl.AddShowData(intTerminalNo.ToString() + "号终端4G点Socket注册", DataLevel.Normal);
                                                            }));
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
                                                    this.dataShowUserCtrl.AddShowData("初始信息配置指令，" + actualReceive[4] + "号4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0xf1:
                                                    this.dataShowUserCtrl.AddShowData("读取单点配置信息指令，" + actualReceive[4] + "号4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0xf2:
                                                    this.dataShowUserCtrl.AddShowData("设置门限指令，" + actualReceive[4] + "号4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0x52:
                                                    this.dataShowUserCtrl.AddShowData("实时时钟配置指令，" + actualReceive[4] + "号4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                //case 0xf3:
                                                //    this.dataShowUserCtrl.AddShowData("超声信号发射通报指令，4G终端已接收！", DataLevel.Normal);
                                                //    break;
                                                case 0xf4:
                                                    this.dataShowUserCtrl.AddShowData("获取Flash里存储的铁轨历史信息指令，" + actualReceive[4] + "号4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0xf5:
                                                    this.dataShowUserCtrl.AddShowData("获取单点铁轨信息指令，" + actualReceive[4] + "号4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0x56:
                                                    this.dataShowUserCtrl.AddShowData("擦除flash指令，" + actualReceive[4] + "号4G终端已接收！", DataLevel.Normal);
                                                    break;
                                                case 0x55:
                                                    this.dataShowUserCtrl.AddShowData("获取某段铁轨信息，" + actualReceive[4] + "号4G终端已接收！", DataLevel.Normal);
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
                                                        if (i == 0 || i == 1)
                                                        {
                                                            if (0 != actualReceive[8])
                                                            {
                                                                AppendMessage(terminalNo.ToString() + "号终端次级相邻小终端不为0！\r\n终端没有次级相邻小终端应填0", DataLevel.Error);
                                                                isError = true;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (MasterControlList[i - 1].NeighbourSmall != actualReceive[8])
                                                            {
                                                                AppendMessage(terminalNo.ToString() + "号终端次级相邻小终端不匹配！\r\nconfig.xml配置文件中为"
                                                                    + MasterControlList[i - 1].NeighbourSmall.ToString() + "收到的为" + actualReceive[8].ToString(), DataLevel.Error);
                                                                isError = true;
                                                            }
                                                        }
                                                        if (masterControl.NeighbourSmall != actualReceive[9])
                                                        {
                                                            AppendMessage(terminalNo.ToString() + "号终端相邻小终端不匹配！\r\nconfig.xml配置文件中为"
                                                                    + masterControl.NeighbourSmall.ToString() + "收到的为" + actualReceive[9].ToString(), DataLevel.Error);
                                                            isError = true;
                                                        }
                                                        if (masterControl.NeighbourBig != actualReceive[10])
                                                        {
                                                            AppendMessage(terminalNo.ToString() + "号终端相邻大终端不匹配！\r\nconfig.xml配置文件中为"
                                                                    + masterControl.NeighbourBig.ToString() + "收到的为" + actualReceive[10].ToString(), DataLevel.Error);
                                                            isError = true;
                                                        }
                                                        if (i == count - 2 || i == count - 1)
                                                        {
                                                            if (0xff != actualReceive[11])
                                                            {
                                                                AppendMessage(terminalNo.ToString() + "号终端次级相邻大终端不为255！\r\n终端没有次级相邻大终端应填255", DataLevel.Error);
                                                                isError = true;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (MasterControlList[i + 1].NeighbourBig != actualReceive[11])
                                                            {
                                                                AppendMessage(terminalNo.ToString() + "号终端次级相邻大终端不匹配！\r\nconfig.xml配置文件中为"
                                                                    + MasterControlList[i + 1].NeighbourBig.ToString() + "收到的为" + actualReceive[11].ToString(), DataLevel.Error);
                                                                isError = true;
                                                            }
                                                        }
                                                        if (!isError)
                                                        {
                                                            bool flashIsValid = false;
                                                            if (actualReceive[12] == 1)
                                                            {
                                                                flashIsValid = true;
                                                            }
                                                            else if (actualReceive[12] == 0)
                                                            {
                                                                flashIsValid = false;
                                                            }
                                                            else
                                                            {
                                                                AppendMessage("‘Flash是否有效’字段收到未定义数据。按照无效处理！", DataLevel.Error);
                                                            }
                                                            PointConfigInfoWindow onePCIWin = new PointConfigInfoWindow(terminalNo, actualReceive[8], actualReceive[9], actualReceive[10], actualReceive[11], flashIsValid);
                                                            onePCIWin.Owner = this;
                                                            onePCIWin.ShowDialog();
                                                        }
                                                        break;
                                                    }
                                                    if (count - 1 == i)
                                                    {
                                                        AppendMessage(terminalNo.ToString() + "号终端不存在", DataLevel.Error);
                                                    }
                                                    i++;
                                                }
                                            }));
                                        }
                                        break;
                                    case 0xf4:
                                        {
                                            //每次检查路径都会把_directoryName置成现在的时间。会导致，每次写前一天的都会进入if (directoryName != _directoryName)判断。
                                            //然后把_hit0xf4Count置0，就会删除上次写好的文件。
                                            //。检查路径已经在refreshFileConfig（）里做了因此不需要在这里检查路径了。
                                            //checkDirectory();
                                            int length = (actualReceive[2] << 8) + actualReceive[3];
                                            if (length == 10 && actualReceive[7] == 0xff)
                                            {
                                                //接收数据结束。
                                                _hit0xf4Count = 0;
                                                string fileNames = String.Empty;
                                                foreach (var item in _fileNameList)
                                                {
                                                    fileNames += "\r\n" + item;
                                                }
                                                if (_fileNameList.Count == 0)
                                                {
                                                    AppendMessage("共写了" + _fileNameList.Count + "个文件", DataLevel.Default);
                                                }
                                                else
                                                {
                                                    AppendMessage("共写了" + _fileNameList.Count + "个文件" + (_fileNameList.Count == 1 ? "为：" : "分别为：") + fileNames, DataLevel.Default);
                                                }
                                                _fileNameList.Clear();
                                            }
                                            else if ((length - 10) % 84 == 0)
                                            {
                                                int dataCount = (length - 10) / 84;
                                                if (dataCount != 0)
                                                {
                                                    bool isReturn = false;
                                                    int jStartValue = 0;
                                                    judgeRefreshFileOrNot:
                                                    if (_hit0xf4Count == 0)
                                                    {
                                                        refreshFileConfig(actualReceive[7]);
                                                    }
                                                    string fileName = System.Environment.CurrentDirectory + @"\History\" + _directoryHistoryName + @"\DataTerminal" + actualReceive[7].ToString("D3") + ".xml";
                                                    if (File.Exists(fileName))
                                                    {
                                                        XmlDocument xmlDoc = new XmlDocument();
                                                        xmlDoc.Load(fileName);
                                                        XmlNode xn1 = xmlDoc.SelectSingleNode("Datas");
                                                        if (xn1 != null)
                                                        {
                                                            int j;
                                                            if (!isReturn)
                                                            {
                                                                j = 0;
                                                            }
                                                            else
                                                            {
                                                                j = jStartValue;
                                                            }
                                                            for (; j < dataCount; j++)
                                                            {
                                                                int year = actualReceive[8 + j * 84] + 2000;
                                                                int month = actualReceive[9 + j * 84];
                                                                int day = actualReceive[10 + j * 84];
                                                                string directoryName = year.ToString() + "\\" + year.ToString() + "-" + month.ToString("D2") + "\\" + year.ToString() + "-" + month.ToString("D2") + "-" + day.ToString("D2");
                                                                if (directoryName != _directoryHistoryName)
                                                                {
                                                                    if (j != 0)
                                                                    {
                                                                        xmlDoc.Save(fileName);
                                                                        if (!_fileNameList.Exists(x => x.Equals(fileName)))
                                                                        {
                                                                            _fileNameList.Add(fileName);
                                                                        }
                                                                    }
                                                                    _directoryHistoryName = directoryName;
                                                                    jStartValue = j;
                                                                    isReturn = true;
                                                                    _hit0xf4Count = 0;
                                                                    goto judgeRefreshFileOrNot;
                                                                }

                                                                //写入xml文件格式。
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

                                                                //写入数据。
                                                                xeTime.InnerText = actualReceive[8 + j * 84].ToString() + "-" + actualReceive[9 + j * 84].ToString() + "-" +
                                                                                   actualReceive[10 + j * 84].ToString() + "-" + actualReceive[11 + j * 84].ToString() + "-" +
                                                                                   actualReceive[12 + j * 84].ToString() + "-" + actualReceive[13 + j * 84].ToString();
                                                                xeOnOff1.InnerText = actualReceive[14 + j * 84].ToString();
                                                                xeStress1.InnerText = actualReceive[16 + j * 84].ToString() + "-" + actualReceive[17 + j * 84].ToString();
                                                                xeTemprature1.InnerText = actualReceive[20 + j * 84].ToString() + "-" + actualReceive[21 + j * 84].ToString();
                                                                xeThisAmplitude1.InnerText = actualReceive[24 + j * 84].ToString() + "-" + actualReceive[25 + j * 84].ToString();
                                                                string strSignalAmplitude = "";
                                                                for (int i = 28 + j * 84; i < 44 + j * 84; i++)
                                                                {
                                                                    strSignalAmplitude += actualReceive[i].ToString();
                                                                    if (i == 43 + j * 84)
                                                                    {
                                                                        continue;
                                                                    }
                                                                    strSignalAmplitude += "-";
                                                                }
                                                                xeSignalAmplitude1Left.InnerText = strSignalAmplitude;
                                                                strSignalAmplitude = "";
                                                                for (int i = 44 + j * 84; i < 60 + j * 84; i++)
                                                                {
                                                                    strSignalAmplitude += actualReceive[i].ToString();
                                                                    if (i == 59 + j * 84)
                                                                    {
                                                                        continue;
                                                                    }
                                                                    strSignalAmplitude += "-";
                                                                }
                                                                xeSignalAmplitude1Right.InnerText = strSignalAmplitude;
                                                                xeOnOff2.InnerText = actualReceive[15 + j * 84].ToString();
                                                                xeStress2.InnerText = actualReceive[18 + j * 84].ToString() + "-" + actualReceive[19 + j * 84].ToString();
                                                                xeTemprature2.InnerText = actualReceive[22 + j * 84].ToString() + "-" + actualReceive[23 + j * 84].ToString();
                                                                //第二个温度前一字节还表示收到了非相邻终端发来的超声信号吗？
                                                                //if (actualReceive[56] == 1)
                                                                //{
                                                                //    this.Dispatcher.Invoke(new Action(() =>
                                                                //    {
                                                                //        this.dataShowUserCtrl.AddShowData(actualReceive[7].ToString() + "号终端收到非相邻终端发来的超声信号！", DataLevel.Warning);
                                                                //    }));
                                                                //}
                                                                //else if (actualReceive[56] == 0)
                                                                //{

                                                                //}
                                                                //else
                                                                //{
                                                                //    AppendMessage("'收到非相邻终端发来的超声信号'位收到未定义数据！");
                                                                //}
                                                                xeThisAmplitude2.InnerText = actualReceive[26 + j * 84].ToString() + "-" + actualReceive[27 + j * 84].ToString();
                                                                strSignalAmplitude = "";
                                                                for (int i = 60 + j * 84; i < 76 + j * 84; i++)
                                                                {
                                                                    strSignalAmplitude += actualReceive[i].ToString();
                                                                    if (i == 75 + j * 84)
                                                                    {
                                                                        continue;
                                                                    }
                                                                    strSignalAmplitude += "-";
                                                                }
                                                                xeSignalAmplitude2Left.InnerText = strSignalAmplitude;
                                                                strSignalAmplitude = "";
                                                                for (int i = 76 + j * 84; i < 92 + j * 84; i++)
                                                                {
                                                                    strSignalAmplitude += actualReceive[i].ToString();
                                                                    if (i == 91 + j * 84)
                                                                    {
                                                                        continue;
                                                                    }
                                                                    strSignalAmplitude += "-";
                                                                }
                                                                xeSignalAmplitude2Right.InnerText = strSignalAmplitude;
                                                            }
                                                        }
                                                        xmlDoc.Save(fileName);
                                                        if (!_fileNameList.Exists(x => x.Equals(fileName)))
                                                        {
                                                            _fileNameList.Add(fileName);
                                                        }
                                                    }
                                                    _hit0xf4Count++;
                                                }
                                            }
                                            else
                                            {
                                                AppendMessage("接收到的数据内容长度不是一包长度84的整数倍！", DataLevel.Error);
                                            }
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
                                                        AppendMessage("'收到非相邻终端发来的超声信号'位收到未定义数据！", DataLevel.Error);
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
                                                RailInfoResultWindow railInfoResultWin = RailInfoResultWindow.GetInstance(actualReceive[7], RailInfoResultMode.获取全部铁轨信息模式);
                                                int index = FindMasterControlIndex(actualReceive[7]);
                                                if (index == -1)
                                                {
                                                    AppendMessage("收到的终端号在终端集合中不存在！", DataLevel.Error);
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
                                            try
                                            {
                                                if (this._svtThumbnail == null)
                                                {
                                                    AppendMessage("设备及铁轨未初始化！", DataLevel.Error);
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
                                                for (int i = 0; i < bytesOnOffContent.Length; i += 10)
                                                {
                                                    for (int j = 0; j < 10; j++)
                                                    {
                                                        bytesTemp[i + j] = bytesOnOffContent[bytesOnOffContent.Length - i - (10 - j)];
                                                    }
                                                }
                                                bytesTemp.CopyTo(bytesOnOffContent, 0);
                                                int contentLength = bytesOnOffContent.Length;
                                                if (contentLength % 10 == 0)
                                                {
                                                    if (contentLength == 10)
                                                    {
                                                        //如果只有一个终端的数据就不存在两个终端数据冲突的情况。
                                                        int index = FindMasterControlIndex(bytesOnOffContent[0]);
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
                                                        MasterControlList[index].Rail1Stress = (bytesOnOffContent[3] << 8) + bytesOnOffContent[4];
                                                        MasterControlList[index].Rail2Stress = (bytesOnOffContent[5] << 8) + bytesOnOffContent[6];
                                                        MasterControlList[index].Rail1Temperature = setMasterCtrlTemperature(bytesOnOffContent[7]);
                                                        MasterControlList[index].Rail2Temperature = setMasterCtrlTemperature(bytesOnOffContent[8]);
                                                        MasterControlList[index].MasterCtrlTemperature = setMasterCtrlTemperature(bytesOnOffContent[9]);
                                                    }
                                                    else
                                                    {
                                                        //如果有多个终端的数据，需要处理冲突。
                                                        for (int i = 0; i < contentLength - 10; i += 10)
                                                        {
                                                            int index = FindMasterControlIndex(bytesOnOffContent[i]);
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
                                                                if (((bytesOnOffContent[i + 1] & 0xf0) >> 4) == (bytesOnOffContent[i + 11] & 0x0f))
                                                                {
                                                                    //不冲突
                                                                    int onOff = (bytesOnOffContent[i + 1] & 0xf0) >> 4;
                                                                    this.Dispatcher.Invoke(new Action(() =>
                                                                    {
                                                                        setRail1State(index, onOff);
                                                                    }));
                                                                }
                                                                else if (((bytesOnOffContent[i + 1] & 0xf0) >> 4) == 9 || (bytesOnOffContent[i + 11] & 0x0f) == 9)
                                                                {
                                                                    this.Dispatcher.Invoke(new Action(() =>
                                                                    {
                                                                        setRail1State(index, 9);
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
                                                                        else if ((bytesOnOffContent[i + 11] & 0x0f) == 0x07)
                                                                        {
                                                                            errorTerminal = tNextNo.ToString() + "号终端接收异常";
                                                                        }
                                                                        this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的1号铁轨通断信息矛盾！" + errorTerminal +
                                                                            "，请检查", DataLevel.Warning);
                                                                    }));
                                                                }
                                                            }
                                                            if (i == (contentLength - 20))
                                                            {
                                                                int indexLastTerminal = FindMasterControlIndex(bytesOnOffContent[i + 10]);
                                                                if (_terminalsReceiveFlag != null)
                                                                {
                                                                    _terminalsReceiveFlag[bytesOnOffContent[i + 10]] = true;
                                                                }
                                                                if (indexLastTerminal != MasterControlList.Count - 1)
                                                                {
                                                                    //最后一个终端没有右边的铁轨
                                                                    int onOffRail1Right = (bytesOnOffContent[i + 11] & 0xf0) >> 4;
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
                                                                if (((bytesOnOffContent[i + 2] & 0xf0) >> 4) == (bytesOnOffContent[i + 12] & 0x0f))
                                                                {
                                                                    //不冲突
                                                                    int onOff = (bytesOnOffContent[i + 2] & 0xf0) >> 4;
                                                                    this.Dispatcher.Invoke(new Action(() =>
                                                                    {
                                                                        setRail2State(index, onOff);
                                                                    }));
                                                                }
                                                                else if (((bytesOnOffContent[i + 2] & 0xf0) >> 4) == 9 || (bytesOnOffContent[i + 12] & 0x0f) == 9)
                                                                {
                                                                    this.Dispatcher.Invoke(new Action(() =>
                                                                    {
                                                                        setRail2State(index, 9);
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
                                                                        else if ((bytesOnOffContent[i + 12] & 0x0f) == 0x07)
                                                                        {
                                                                            errorTerminal = tNextNo.ToString() + "号终端接收异常";
                                                                        }
                                                                        this.dataShowUserCtrl.AddShowData(tNo.ToString() + "号终端与" + tNextNo.ToString() + "号终端之间的2号铁轨通断信息矛盾！" + errorTerminal +
                                                                            "，请检查", DataLevel.Warning);
                                                                    }));
                                                                }
                                                            }
                                                            if (i == (contentLength - 20))
                                                            {
                                                                int indexLastTerminal = FindMasterControlIndex(bytesOnOffContent[i + 10]);
                                                                if (indexLastTerminal != MasterControlList.Count - 1)
                                                                {
                                                                    //最后一个终端没有右边的铁轨
                                                                    int onOffRail2Right = (bytesOnOffContent[i + 12] & 0xf0) >> 4;
                                                                    this.Dispatcher.Invoke(new Action(() =>
                                                                    {
                                                                        setRail2State(indexLastTerminal, onOffRail2Right);
                                                                    }));
                                                                }
                                                            }
                                                            MasterControlList[index].Rail1Stress = (bytesOnOffContent[i + 3] << 8) + bytesOnOffContent[i + 4];
                                                            MasterControlList[index].Rail2Stress = (bytesOnOffContent[i + 5] << 8) + bytesOnOffContent[i + 6];
                                                            MasterControlList[index].Rail1Temperature = setMasterCtrlTemperature(bytesOnOffContent[i + 7]);
                                                            MasterControlList[index].Rail2Temperature = setMasterCtrlTemperature(bytesOnOffContent[i + 8]);
                                                            MasterControlList[index].MasterCtrlTemperature = setMasterCtrlTemperature(bytesOnOffContent[i + 9]);
                                                            if (i == (contentLength - 20))
                                                            {
                                                                index = FindMasterControlIndex(bytesOnOffContent[i + 10]);
                                                                MasterControlList[index].Rail1Stress = (bytesOnOffContent[i + 13] << 8) + bytesOnOffContent[i + 14];
                                                                MasterControlList[index].Rail2Stress = (bytesOnOffContent[i + 15] << 8) + bytesOnOffContent[i + 16];
                                                                MasterControlList[index].Rail1Temperature = setMasterCtrlTemperature(bytesOnOffContent[i + 17]);
                                                                MasterControlList[index].Rail2Temperature = setMasterCtrlTemperature(bytesOnOffContent[i + 18]);
                                                                MasterControlList[index].MasterCtrlTemperature = setMasterCtrlTemperature(bytesOnOffContent[i + 19]);
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
                                                    AppendMessage("发送数据内容的长度错误，应该是10的倍数", DataLevel.Error);
                                                }
                                            }
                                            catch (Exception ee)
                                            {
                                                AppendMessage("处理常规信息异常：" + ee.Message, DataLevel.Error);
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
                                    //        AppendMessage(errorTerminalNo.ToString() + " 号终端回应超时！");
                                    //    }
                                    //    break;
                                    default:
                                        AppendMessage("收到未知数据！", DataLevel.Error);
                                        break;
                                }
                            }
                            else if (actualReceive[0] == 0x55 && actualReceive[1] == 0xaa)
                            {
                                switch (actualReceive[5])
                                {
                                    case (byte)CommandType.AssignClientID:
                                        {
                                            this.Dispatcher.BeginInvoke(new Action(() =>
                                            {
                                                this.clientIDShow.ClientID = actualReceive[4];
                                                this.dataShowUserCtrl.AddShowData("为电脑分配ClientID：" + actualReceive[4].ToString(), DataLevel.Default);
                                            }));
                                        }
                                        break;
                                    case (byte)CommandType.BroadcastConfigFileSize:
                                        {
                                            handleBroadcastFileSize(actualReceive);
                                        }
                                        break;
                                    default:
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
                //IPEndPoint deviceIP = new IPEndPoint(IPAddress.Parse("192.168.16.254"), 8080);
                //IPEndPoint deviceIP = new IPEndPoint(IPAddress.Parse("103.44.145.248"), 23539);
                //IPEndPoint deviceIP = new IPEndPoint(IPAddress.Parse("192.168.1.106"), 23539);
                //IPEndPoint deviceIP = new IPEndPoint(IPAddress.Parse("103.44.145.233"), 23539);

                IPHostEntry host = Dns.GetHostEntry(_serverWeb);
                IPAddress ip = host.AddressList[0];
                _serverIP = ip.ToString();
                IPEndPoint deviceIP = new IPEndPoint(ip, 23539);
                _socketMain = new Socket(deviceIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socketMain.Connect(deviceIP);

                _isConnect = true;
                this.miConnect.Header = "已连接";
                this.miConnect.Background = new SolidColorBrush(Colors.LightGreen);
                this.miConnect.IsEnabled = false;
                SendData("电脑" + _socketMain.LocalEndPoint.ToString());

                _socketListeningThread = new Thread(new ParameterizedThreadStart(socketListening));
                _socketListeningThread.Start(_socketMain);
                //IPEndPoint hostIP = new IPEndPoint(IPAddress.Any, 16479);
                //_socketMain = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //_socketMain.Bind(hostIP);
                //_socketMain.Listen(500);
                //_socketAcceptThread = new Thread(socketAccept);
                //_socketAcceptThread.IsBackground = true;
                //_socketAcceptThread.Start();
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
                    this.miSubscribeAllRailInfo.Header = "订阅所有终端铁轨信息";
                    errorAllRails();
                    _isSubscribingAllRailInfo = false;
                }
                else
                {
                    sendData = SendDataPackage.PackageSendData((byte)this.clientIDShow.ClientID,
                            (byte)0xff, (byte)CommandType.SubscribeAllRailInfo, new byte[1] { 0 });
                    this.miSubscribeAllRailInfo.Header = "取消订阅所有终端铁轨信息";
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
    }
}
