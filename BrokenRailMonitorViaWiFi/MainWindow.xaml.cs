using BrokenRail3MonitorViaWiFi.Classes;
using BrokenRail3MonitorViaWiFi.UserControls;
using BrokenRail3MonitorViaWiFi.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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
using Floatable = FloatableUserControl;

namespace BrokenRail3MonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _directoryName;
        private string _directoryHistoryName;
        private Socket _socketMain;
        private Socket _acceptSocket;
        private Thread _socketListeningThread;
        private List<MasterControl> _masterControlList = new List<MasterControl>();
        private List<Rail> _rail1List = new List<Rail>();
        private List<Rail> _rail2List = new List<Rail>();
        private DispatcherTimer _getAllRailInfoTimer = new DispatcherTimer();
        private DispatcherTimer _waitReceiveTimer = new DispatcherTimer();
        private DispatcherTimer _timeToWaitTimer = new DispatcherTimer();
        private DispatcherTimer _realTimeRefreshTimer = new DispatcherTimer();
        private int _packageCount = 0;
        private int _receiveEmptyPackageCount = 0;
        private List<int> _socketRegister = new List<int>();
        private List<int> _4GPointIndex = new List<int>();
        private List<int> _sendTime = new List<int>();
        //private int _hit0xf4Count = 0;
        private List<string> _fileNameList = new List<string>();
        private bool _isConnect = false;
        //private const String _serverWeb = "f1880f0253.51mypc.cn";
        private const String _serverWeb = "terrytec.iok.la";
        private const int _fileReceivePort = 23955;
        private Socket _socket;
        private static MainWindow _instance;
        private ConfigInfoWindow _winConfigInfo;
        private TongDuanWindow _winTongDuan;
        private FFTWindow _winFFT;

        private FFTUserControl _ucFFT = new FFTUserControl(new byte[4122]);
        private TongDuanUserControl _ucTongDuan = new TongDuanUserControl();
        private List<Floatable.FloatableUserControl> _floatUserCtrlList = new List<Floatable.FloatableUserControl>();
        private DataShowUserControl dataShowUserCtrl = new DataShowUserControl();
        private DebugMsgUserControl _msgUserControl = new DebugMsgUserControl();
        private string _realTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
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

        public ConfigInfoUserControl UCConfigInfo { get; set; }

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

        public string RealTime
        {
            get => _realTime;
            set
            {
                _realTime = value;
                OnPropertyChanged("RealTime");
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

            //WaitReceiveTimer.Tick += WaitReceiveTimer_Tick;
            //WaitReceiveTimer.Interval = new TimeSpan(0, 0, 20);

            //_multicastWaitReceiveTimer.Tick += multicastWaitReceiveTimer_Tick;
            //_multicastWaitReceiveTimer.Interval = new TimeSpan(0, 0, 20);
            //_waitingRingThread = new Thread(waitingRingEnable);
            _realTimeRefreshTimer.Tick += RealTimeRefreshTimer_Tick;
            _realTimeRefreshTimer.Interval = new TimeSpan(0, 0, 1);
            _realTimeRefreshTimer.Start();
            _instance = this;
            DataContext = this;
        }

        private void RealTimeRefreshTimer_Tick(object sender, EventArgs e)
        {
            RealTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        }

        private void WinFFT_Closed(object sender, EventArgs e)
        {
            _winFFT = null;
        }
        private void WinTongDuan_Closed(object sender, EventArgs e)
        {
            _winTongDuan = null;
        }

        private void WinConfigInfo_Closed(object sender, EventArgs e)
        {
            _winConfigInfo = null;
        }

        public static MainWindow GetInstance()
        {
            return _instance;
        }

        private void devicesInitial()
        {
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
                    byte[] checkSumErrorArray = null;
                    int hitLevel = 0;
                    while (true)
                    {
                        Thread.Sleep(100);
                        byte[] receivedBytes = new byte[5120];
                        int rightStartIndex = 0;
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
                            if (hitLevel == 0 && receivedBytes[i] == 0x66)
                            {
                                hitLevel = 1;
                            }
                            else if (hitLevel == 1 && receivedBytes[i] == 0xcc)
                            {
                                hitLevel = 2;
                                if (rightStartIndex == 0)
                                {
                                    rightStartIndex = accumulateNumber + i - 1;
                                }
                            }
                            else
                            {
                                hitLevel = 0;
                            }
                        }
                    //处理断包
                    //V519发满400字节之后会截断一下，在下一个400字节继续发送
                    //WiFi发满1024字节之后会截断一下，在下一个1024字节继续发送
                    //long beforePlusRemainder = accumulateNumber % 1024;
                    duanBao:
                        accumulateNumber += numBytes;
                        int afterPlusRemainder = accumulateNumber % 1024;
                        if (afterPlusRemainder == 0)
                        {
                            //等于0的时候说明接收的字段跨过1024字节，再收一组数据。
                            //有一种特殊情况，就是收到1024字节的时候正好是一整包，这样进入判断的话就会将两个本来就应该分开的包连起来，这种情况没有处理。
                            receivedBytes = new byte[5120];
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
                            goto duanBao;
                        }
                        else
                        {
                            accumulateNumber = 0;
                        }

                        byte[] packageUnhandled = new byte[0];

                    handlePackage: ASCIIEncoding encoding = new ASCIIEncoding();
                        string strReceive = encoding.GetString(actualReceive);
                        if (strReceive.Length > 0)
                        {
                            //检查校验和
                            try
                            {
                                if (rightStartIndex != 0)
                                {
                                    //处理粘包的情况。并且第一个字节不是枕头
                                    int unhandledLength = actualReceive.Length - rightStartIndex;
                                    byte[] packagePrevious = new byte[rightStartIndex];
                                    packageUnhandled = new byte[unhandledLength];
                                    for (int j = 0; j < rightStartIndex; j++)
                                    {
                                        packagePrevious[j] = actualReceive[j];
                                    }
                                    for (int i = 0; i < unhandledLength; i++)
                                    {
                                        packageUnhandled[i] = actualReceive[rightStartIndex + i];
                                    }
                                    actualReceive = new byte[rightStartIndex];
                                    packagePrevious.CopyTo(actualReceive, 0);
                                    rightStartIndex = 0;
                                    goto handlePackage;
                                }
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

                                    int length = (actualReceive[3] << 8) + actualReceive[4] + 2;
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
                                    for (int i = 2; i < actualReceive.Length - 2; i++)
                                    {
                                        checksum += actualReceive[i];
                                    }
                                    int sumHigh;
                                    int sumLow;
                                    sumHigh = (checksum & 0xff00) >> 8;
                                    sumLow = checksum & 0xff;
                                    if (sumHigh != actualReceive[actualReceive.Length - 2] || sumLow != actualReceive[actualReceive.Length - 1])
                                    {
                                        checkSumErrorArray = actualReceive;
                                        this.Dispatcher.Invoke(new Action(() =>
                                        {
                                            this.dataShowUserCtrl.AddShowData("校验和出错", DataLevel.Error);
                                        }));
                                        continue;
                                    }
                                    else
                                    {
                                        checkSumErrorArray = null;
                                    }
                                    switch (actualReceive[2])
                                    {
                                        //获取配置
                                        case 0x02:
                                            {
                                                this.Dispatcher.Invoke(new Action(() =>
                                                {
                                                    handleGetConfig(actualReceive);
                                                }));
                                            }
                                            break;
                                        //实时通断数据
                                        case 0xA0:
                                            {
                                                this.Dispatcher.Invoke(new Action(() =>
                                                {
                                                    handleRealtimeAmpData(actualReceive);
                                                }));
                                            }
                                            break;
                                        //实时频谱信息
                                        case 0xA1:
                                            {
                                                this.Dispatcher.Invoke(new Action(() =>
                                                {
                                                    handleRealtimeSpectrum(actualReceive);
                                                }));
                                            }
                                            break;
                                        default:
                                            AppendMessage("收到未知数据！", DataLevel.Error);
                                            break;
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
                                    if (checkSumErrorArray != null)
                                    {
                                        byte[] sumReceive = new byte[checkSumErrorArray.Length + actualReceive.Length];
                                        checkSumErrorArray.CopyTo(sumReceive, 0);
                                        actualReceive.CopyTo(sumReceive, checkSumErrorArray.Length);
                                        actualReceive = new byte[sumReceive.Length];
                                        sumReceive.CopyTo(actualReceive, 0);
                                        AppendMessage("拆分组合", DataLevel.Warning);
                                        goto handlePackage;
                                    }
                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        //string strReceiveBroken = encoding.GetString(actualReceive);
                                        StringBuilder sb = new StringBuilder(500);
                                        for (int i = 0; i < actualReceive.Length; i++)
                                        {
                                            sb.Append(actualReceive[i].ToString("x2"));
                                        }
                                        //this.dataShowUserCtrl.AddShowData("收到拆分的错误包  (长度：" + actualReceive.Length.ToString() + ")  " + sb.ToString(), DataLevel.Warning);
                                        _msgUserControl.AppendMsg(System.Text.Encoding.GetEncoding("gb2312").GetString(actualReceive));
                                    }));
                                    //continue;
                                }
                                if (packageUnhandled.Length != 0)
                                {
                                    actualReceive = new byte[packageUnhandled.Length];
                                    packageUnhandled.CopyTo(actualReceive, 0);
                                    packageUnhandled = new byte[0];
                                    goto handlePackage;
                                }
                            }
                            catch (Exception ee)
                            {
                                AppendMessage("检查校验和异常：" + ee.Message, DataLevel.Error);
                            }
                        }
                        else
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                this.dataShowUserCtrl.AddShowData("接收数据长度小于0  " + strReceive, DataLevel.Warning);
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
                        break;
                    default:
                        AppendMessage("发生未处理异常！", DataLevel.Error);
                        break;
                }
                this.Dispatcher.Invoke(new Action(() =>
                {
                    connectCloseHandle(_socket);
                }));
            }
            catch (Exception ee)
            {
                //socketDisconnect();
                AppendMessage("Socket监听线程异常：" + ee.Message, DataLevel.Error);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    connectCloseHandle(_socket);
                }));
            }
        }

        private void handleRealtimeSpectrum(byte[] data)
        {
            _ucFFT.SetData(data);
        }


        private void handleRealtimeAmpData(byte[] data)
        {
            _ucTongDuan.SetData(data);
        }

        private void handleGetConfig(byte[] data)
        {
            if (_winConfigInfo == null)
            {
                _winConfigInfo = new ConfigInfoWindow();
                _winConfigInfo.Closed += WinConfigInfo_Closed;
                _winConfigInfo.SetData(data);
                _winConfigInfo.Show();
            }
            else
            {
                _winConfigInfo.SetData(data);
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
        }

        private void setRail2State(int index, int onOff)
        {
        }

        private void errorAllRails()
        {

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
                //IPEndPoint hostIP = new IPEndPoint(IPAddress.Parse("192.168.0.201"), 8234);
                //_socketMain = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //_socketMain.Bind(hostIP);
                //_socketMain.Listen(500);
                //_acceptSocket = _socketMain.Accept();

                IPEndPoint hostIP = new IPEndPoint(IPAddress.Parse("192.168.4.1"), 333);
                _acceptSocket = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _acceptSocket.Connect(hostIP);

                _socketListeningThread = new Thread(new ParameterizedThreadStart(socketListening));
                _socketListeningThread.Start(_acceptSocket);

                _isConnect = true;
                this.miConnect.Header = "已连接";
                this.miConnect.Background = new SolidColorBrush(Colors.LightGreen);
                this.miConnect.IsEnabled = false;
                this.miDisconnect.IsEnabled = true;
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
                        this.miDisconnect.IsEnabled = true;
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
            if (_socketMain != null)
            {
                _socketMain.Close();
                _socketMain = null;
            }
            if (_acceptSocket != null)
            {
                _acceptSocket.Close();
                _acceptSocket = null;
            }
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.miConnect.Header = "连接";
                this.miConnect.IsEnabled = true;
                this.miConnect.Background = new SolidColorBrush((this.miCommand.Background as SolidColorBrush).Color);
                this.miDisconnect.IsEnabled = false;
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

        }

        private void miSubscribeAllRailInfo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void WaitReceiveTimer_Tick(object sender, EventArgs e)
        {
            this.WaitingRingDisable();
            this.WaitReceiveTimer.Stop();
            AppendMessage("超过20秒未收到数据，连接可能已断开！", DataLevel.Error);
        }
        private void multicastWaitReceiveTimer_Tick(object sender, EventArgs e)
        {
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
        }
        private void miGetOneSectionInfo_Click(object sender, RoutedEventArgs e)
        {
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
        }

        public void WaitingRingDisable()
        {
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
        }

        private void miDownload_Click(object sender, RoutedEventArgs e)
        {
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
            try
            {
                //显示版本信息
                string version = "  v" + GetCurrentApplicationVersion();
                this.Title += version;
                fucTongDuan.Closed += FucTongDuan_Closed;
                fucFFT.Closed += FucFFT_Closed;
                fucMessage.Closed += FucMessage_Closed;
                fucDebugMsg.Closed += FucDebugMsg_Closed;

                fucTongDuan.GridContainer.Children.Add(_ucTongDuan);
                fucFFT.GridContainer.Children.Add(_ucFFT);
                fucMessage.GridContainer.Children.Add(dataShowUserCtrl);
                fucDebugMsg.GridContainer.Children.Add(_msgUserControl);

                //fucMessage.GridContainer.Background = new SolidColorBrush(Colors.White);

                _ucTongDuan.MouseLeftButtonDown += UcTongDuan_MouseLeftButtonDown;
                _ucFFT.MouseLeftButtonDown += UcFFT_MouseLeftButtonDown;
                dataShowUserCtrl.MouseLeftButtonDown += DataShowUserCtrl_MouseLeftButtonDown;
                _msgUserControl.MouseLeftButtonDown += MsgUserControl_MouseLeftButtonDown;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void MsgUserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            fucDebugMsg.FocusTitleRect();
        }

        private void FucDebugMsg_Closed()
        {
            if (!_floatUserCtrlList.Contains(fucDebugMsg))
            {
                _floatUserCtrlList.Add(fucDebugMsg);
            }
        }

        private void DataShowUserCtrl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            fucMessage.FocusTitleRect();
        }

        private void UcFFT_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            fucFFT.FocusTitleRect();
        }

        private void UcTongDuan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            fucTongDuan.FocusTitleRect();
        }

        private void FucMessage_Closed()
        {
            if (!_floatUserCtrlList.Contains(fucMessage))
            {
                _floatUserCtrlList.Add(fucMessage);
            }
        }

        private void FucFFT_Closed()
        {
            if (!_floatUserCtrlList.Contains(fucFFT))
            {
                _floatUserCtrlList.Add(fucFFT);
            }
        }

        private void FucTongDuan_Closed()
        {
            if (!_floatUserCtrlList.Contains(fucTongDuan))
            {
                _floatUserCtrlList.Add(fucTongDuan);
            }
        }

        private string GetCurrentApplicationVersion()
        {

            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            string versionStr = string.Format(" {0}.{1}.{2}", fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.ProductBuildPart);
            return versionStr;
        }

        private void miRealtimeInfo_Click(object sender, RoutedEventArgs e)
        {
            //TongDuanWindow winTongDuan = new TongDuanWindow();
            //winTongDuan.RefreshCharts(new byte[112] { 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x20, 0x00, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78,
            //                                         0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x20, 0x00, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x00, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78,
            //                                          0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x20, 0x00, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x00, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78,
            //                                             0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x20, 0x00, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x00, 0x34, 0x56, 0x78}, 0);
            //winTongDuan.Show();
            //_winFFT = new FFTWindow(new byte[4122]);
            //_winFFT.Closed += WinFFT_Closed; ;
            //_winFFT.Show();
            _winConfigInfo = new ConfigInfoWindow();
            _winConfigInfo.SetData(new byte[97] { 0x66, 0xCC, 0x02, 0x00, 0x5f, 0x5F, 0xB6, 0x32,
                0x49, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2E, 0x00, 0x00, 0x00, 0x00, 0x4C,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x2E, 0x00, 0x00, 0x00, 0x00, 0x4C, 0x00, 0x30, 0x00,
                0x5F, 0xB6, 0x32, 0x49, 0x00, 0x00, 0x30, 0x70, 0x70, 0x64, 0x63, 0x00, 0x00, 0x80,
                0x00, 0x00, 0x00, 0x80, 0x00, 0x25, 0x00, 0x55, 0x00, 0x1E, 0x05, 0x00, 0x14, 0x5F,
                0xB6, 0x2F, 0x06, 0x00, 0x00, 0x03, 0x53, 0x02, 0x06, 0x04, 0x1F, 0x01, 0xEC, 0x01,
                0x47, 0x00, 0x94, 0x01, 0x1B, 0x05, 0xD8, 0xFF, 0x34, 0x05, 0x75, 0x06, 0xD5, 0xFF,
                0xFF, 0x01, 0x00, 0x11, 0x3D });
            _winConfigInfo.Show();
        }

        private void miGetConfigInfo_Click(object sender, RoutedEventArgs e)
        {
            byte[] sendData = SendDataPackage.PackageSendData(Command3Type.SendCmd, ConfigType.GET_CONFIG, 1, new byte[1] { 0 });

            try
            {
                if (_acceptSocket != null)
                {
                    _acceptSocket.Send(sendData, SocketFlags.None);
                    AppendDataMsg(sendData);
                    AppendMessage("获取系统配置信息", DataLevel.Default);
                }
                else
                {
                    AppendMessage("请先连接", DataLevel.Error);
                }
            }
            catch (Exception ee) { AppendMessage(ee.Message, DataLevel.Error); }
        }

        private void miSetTime_Click(object sender, RoutedEventArgs e)
        {
            long stamp = TimeStamp.GetTimeStamp(DateTime.Now);
            byte[] timeArray = new byte[4];
            timeArray[0] = (byte)((stamp & 0xff000000) >> 24);
            timeArray[1] = (byte)((stamp & 0xff0000) >> 16);
            timeArray[2] = (byte)((stamp & 0xff00) >> 8);
            timeArray[3] = (byte)(stamp & 0xff);

            byte[] sendData = SendDataPackage.PackageSendData(Command3Type.SendCmd, ConfigType.SET_Time, 4, timeArray);


            try
            {
                if (_acceptSocket != null)
                {
                    _acceptSocket.Send(sendData, SocketFlags.None);
                    AppendDataMsg(sendData);
                    AppendMessage("对时", DataLevel.Default);
                }
                else
                {
                    AppendMessage("请先连接", DataLevel.Error);
                }
            }
            catch (Exception ee) { AppendMessage(ee.Message, DataLevel.Error); }
        }

        public void SendCommand(byte[] sendData)
        {
            try
            {
                if (_acceptSocket != null)
                {
                    _acceptSocket.Send(sendData, SocketFlags.None);
                    AppendDataMsg(sendData);
                }
                else
                {
                    AppendMessage("请先连接", DataLevel.Error);
                }
            }
            catch (Exception ee)
            {
                AppendMessage(ee.Message, DataLevel.Error);
            }
        }

        private void miSetConfig_Click(object sender, RoutedEventArgs e)
        {
            SetConfigWindow winSetConfig = new SetConfigWindow();
            winSetConfig.Show();
        }

        private void miSetTerminalNum_Click(object sender, RoutedEventArgs e)
        {
            CmdTNumWindow winSendCmd = new CmdTNumWindow();
            winSendCmd.Show();
        }

        private void miDetailConfig_Click(object sender, RoutedEventArgs e)
        {
            CmdDetailConfigWindow winSendCmd = new CmdDetailConfigWindow();
            winSendCmd.Show();
        }

        public ConfigInfoWindow GetConfigInfoWin()
        {
            return _winConfigInfo;
        }

        private void miView_Click(object sender, RoutedEventArgs e)
        {

        }

        private void miTongDuan_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _floatUserCtrlList)
            {
                if (item.Name == "fucTongDuan")
                {
                    item.AddControlAndSetGrid();
                    item.State = Floatable.UserControlState.Dock;
                    _floatUserCtrlList.Remove(item);
                    break;
                }
            }
            fucTongDuan.FocusTitleRect();
        }

        private void miFFT_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _floatUserCtrlList)
            {
                if (item.Name == "fucFFT")
                {
                    item.AddControlAndSetGrid();
                    item.State = Floatable.UserControlState.Dock;
                    _floatUserCtrlList.Remove(item);
                    break;
                }
            }
            fucFFT.FocusTitleRect();
        }

        private void miMessage_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _floatUserCtrlList)
            {
                if (item.Name == "fucMessage")
                {
                    item.AddControlAndSetGrid();
                    item.State = Floatable.UserControlState.Dock;
                    _floatUserCtrlList.Remove(item);
                    break;
                }
            }
            fucMessage.FocusTitleRect();
        }
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        private void miDisconnect_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                connectCloseHandle(_socket);
            }));
        }
    }
}
