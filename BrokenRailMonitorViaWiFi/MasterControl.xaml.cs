using BrokenRail3MonitorViaWiFi.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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

namespace BrokenRail3MonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for MasterControl.xaml
    /// </summary>
    public partial class MasterControl : UserControl, INotifyPropertyChanged
    {
        private int _terminalNumber;
        private Socket _socketImport;
        private int _neighbourSmall;
        private int _neighbourBig;
        private bool _isEnd;
        private string _ipAndPort;
        private string _find4GErrorMsg;
        private MainWindow _mainWin;
        public static readonly DependencyProperty Is4GProperty = DependencyProperty.Register("Is4G", typeof(bool), typeof(MasterControl), new PropertyMetadata(false, OnIs4GChanged));
        private DispatcherTimer _offlineTimer;
        private int _rail1Stress;
        private int _rail1Temperature;
        private int _rail2Stress;
        private int _rail2Temperature;
        private int _masterCtrlTemperature;

        public bool Is4G
        {
            get { return (bool)GetValue(Is4GProperty); }
            set { SetValue(Is4GProperty, value); }
        }
        public int TerminalNumber
        {
            get
            {
                return _terminalNumber;
            }

            set
            {
                if (_terminalNumber != value)
                {
                    _terminalNumber = value;
                    OnPropertyChanged("TerminalNumber");
                }
            }
        }

        public Socket SocketImport
        {
            get
            {
                return _socketImport;
            }

            set
            {
                _socketImport = value;
                if (_socketImport != null)
                {
                    IpAndPort = _socketImport.RemoteEndPoint.ToString();
                }
                else
                {
                    IpAndPort = string.Empty;
                }
            }
        }

        public int NeighbourSmall
        {
            get
            {
                return _neighbourSmall;
            }

            set
            {
                _neighbourSmall = value;
            }
        }

        public int NeighbourBig
        {
            get
            {
                return _neighbourBig;
            }

            set
            {
                _neighbourBig = value;
            }
        }

        public bool IsEnd
        {
            get
            {
                return _isEnd;
            }

            set
            {
                _isEnd = value;
            }
        }

        public string IpAndPort
        {
            get
            {
                return _ipAndPort;
            }

            set
            {
                _ipAndPort = value;
            }
        }

        public string Find4GErrorMsg
        {
            get
            {
                return _find4GErrorMsg;
            }

            set
            {
                _find4GErrorMsg = value;
            }
        }
        public int Rail1Stress
        {
            get
            {
                return _rail1Stress;
            }

            set
            {
                if (_rail1Stress != value)
                {
                    _rail1Stress = value;
                    OnPropertyChanged("Rail1Stress");
                }
            }
        }

        public int Rail1Temperature
        {
            get
            {
                return _rail1Temperature;
            }

            set
            {
                if (_rail1Temperature != value)
                {
                    _rail1Temperature = value;
                    OnPropertyChanged("Rail1Temperature");
                }
            }
        }

        public int Rail2Stress
        {
            get
            {
                return _rail2Stress;
            }

            set
            {
                if (_rail2Stress != value)
                {
                    _rail2Stress = value;
                    OnPropertyChanged("Rail2Stress");
                }
            }
        }

        public int Rail2Temperature
        {
            get
            {
                return _rail2Temperature;
            }

            set
            {
                if (_rail2Temperature != value)
                {
                    _rail2Temperature = value;
                    OnPropertyChanged("Rail2Temperature");
                }
            }
        }

        public int MasterCtrlTemperature
        {
            get
            {
                return _masterCtrlTemperature;
            }

            set
            {
                if (_masterCtrlTemperature != value)
                {
                    _masterCtrlTemperature = value;
                    OnPropertyChanged("MasterCtrlTemperature");
                }
            }
        }

        public MasterControl()
        {
            InitializeComponent();
        }
        public MasterControl(MainWindow mainWin)
        {
            InitializeComponent();
            _mainWin = mainWin;
            this.DataContext = this;
            _offlineTimer = new DispatcherTimer();
            _offlineTimer.Interval = new TimeSpan(0, 2, 5);
            _offlineTimer.Tick += offlineTimer_Tick;
        }

        private void offlineTimer_Tick(object sender, EventArgs e)
        {
            _mainWin.AppendMessage("终端" + TerminalNumber + "超过2分钟没有收到心跳包，可能已经下线", DataLevel.Error);
            Offline();
        }
        private static void OnIs4GChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                ((MasterControl)d).cvs4G.Visibility = Visibility.Visible;
            }
            else
            {
                ((MasterControl)d).cvs4G.Visibility = Visibility.Collapsed;
            }
        }
        private void miGetPointRailInfo_Click(object sender, RoutedEventArgs e)
        {
        }

        //private void miGetPointSignalAmplitude_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        if (SocketImport != null)
        //        {
        //            byte[] sendData = SendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf3, new byte[2] { 0, 0 });
        //            SocketImport.Send(sendData, SocketFlags.None);
        //        }
        //        else
        //        {
        //            MessageBox.Show("Socket未导入！");
        //        }
        //    }
        //    catch (Exception ee)
        //    {
        //        MessageBox.Show(ee.Message);
        //    }
        //}

        private void miConfigInitialInfo_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void miReadPointInfo_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void miSignalSendConfig_Click(object sender, RoutedEventArgs e)
        {
        }

        private void miThresholdSetting_Click(object sender, RoutedEventArgs e)
        {
        }

        private void miGetHistory_Click(object sender, RoutedEventArgs e)
        {
        }

        public Socket GetNearest4GTerminalSocket(bool isForward)
        {
            if (this.Is4G)
            {
                if (_mainWin != null)
                {
                    for (int j = 0; j < _mainWin.SocketRegister.Count; j++)
                    {
                        if (_mainWin.SocketRegister[j] == this.TerminalNumber)
                        {
                            return this.SocketImport;
                        }
                        else if (j == _mainWin.SocketRegister.Count - 1)
                        {
                            break;
                        }
                    }
                    Find4GErrorMsg = "该终端本身为4G点，但此4G点的Socket连接未注册！";
                    return null;
                }
                Find4GErrorMsg = "主窗口句柄为空！";
                return null;
            }
            else
            {
                if (_mainWin != null)
                {
                    if (_mainWin.SocketRegister.Count == 0)
                    {
                        Find4GErrorMsg = "注册的4G点Socket连接个数为0";
                        return null;
                    }
                    else if (_mainWin.SocketRegister.Count == 1)
                    {
                        foreach (var item in _mainWin.MasterControlList)
                        {
                            if (item.TerminalNumber == _mainWin.SocketRegister[0])
                            {
                                return item.SocketImport;
                            }
                        }
                    }
                    else
                    {
                        if (isForward)
                        {
                            //如果是正向
                            int indexOfThisMasterControl = _mainWin.MasterControlList.FindIndex(FindMasterControl);
                            for (int i = indexOfThisMasterControl; i >= 0; i--)
                            {
                                if (_mainWin.MasterControlList[i].Is4G)
                                {
                                    int terminal4GNo = _mainWin.MasterControlList[i].TerminalNumber;
                                    for (int j = 0; j < _mainWin.SocketRegister.Count; j++)
                                    {
                                        if (_mainWin.SocketRegister[j] == terminal4GNo)
                                        {
                                            return _mainWin.MasterControlList[i].SocketImport;
                                        }
                                        else if (j == _mainWin.SocketRegister.Count - 1)
                                        {
                                            Find4GErrorMsg = "正向未找到小于该终端号的4G点Socket连接！";
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //如果是反向
                            int indexOfThisMasterControl = _mainWin.MasterControlList.FindIndex(FindMasterControl);
                            for (int i = indexOfThisMasterControl; i < _mainWin.MasterControlList.Count; i++)
                            {
                                if (_mainWin.MasterControlList[i].Is4G)
                                {
                                    int terminal4GNo = _mainWin.MasterControlList[i].TerminalNumber;
                                    for (int j = 0; j < _mainWin.SocketRegister.Count; j++)
                                    {
                                        if (_mainWin.SocketRegister[j] == terminal4GNo)
                                        {
                                            return _mainWin.MasterControlList[i].SocketImport;
                                        }
                                        else if (j == _mainWin.SocketRegister.Count - 1)
                                        {
                                            Find4GErrorMsg = "反向未找到大于该终端号的4G点Socket连接！";
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Find4GErrorMsg = "主窗口句柄为空！";
                return null;
            }
        }
        private bool FindMasterControl(MasterControl mc)
        {
            if (mc.TerminalNumber == this.TerminalNumber)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void HideContextMenu()
        {
            this.contextMenu.Visibility = Visibility.Hidden;
        }

        public void AppendDataMsg(byte[] sendData)
        {
            StringBuilder sb = new StringBuilder(500);
            for (int i = 0; i < sendData.Length; i++)
            {
                sb.Append(sendData[i].ToString("x2"));
            }
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            //    _mainWin.dataShowUserCtrl.AddShowData("发送数据  (长度：" + sendData.Length.ToString() + ")  " + sb.ToString(), DataLevel.Default);
            //}));
        }
        public void Online()
        {
            if (_offlineTimer.IsEnabled)
            {
                _offlineTimer.Stop();
                _offlineTimer.Start();
            }
            else
            {
                _offlineTimer.Start();
            }
            this.path4G.Fill = new SolidColorBrush(Colors.LightGreen);
        }

        public void Offline()
        {
            this.path4G.Fill = new SolidColorBrush(Colors.Red);
            if (_offlineTimer.IsEnabled)
                _offlineTimer.Stop();
        }
        public void Dispose()
        {
            try
            {
                if (_offlineTimer != null && _offlineTimer.IsEnabled)
                    _offlineTimer.Stop();
                if (this.SocketImport != null)
                {
                    this.SocketImport = null;
                }
                //if (Terminal != null)
                //{
                //    Terminal.Dispose();
                //}
            }
            catch
            {
                throw;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
