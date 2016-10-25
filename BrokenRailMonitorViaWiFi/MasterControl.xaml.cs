using BrokenRailMonitorViaWiFi.Windows;
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

namespace BrokenRailMonitorViaWiFi
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
        private SendDataPackage _sendDataPackage = new SendDataPackage();
        private MainWindow _mainWin;
        public static readonly DependencyProperty Is4GProperty = DependencyProperty.Register("Is4G", typeof(bool), typeof(MasterControl), new PropertyMetadata(false, OnIs4GChanged));

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
                IpAndPort = _socketImport.RemoteEndPoint.ToString();
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

        public MasterControl()
        {
            InitializeComponent();
        }
        public MasterControl(MainWindow mainWin)
        {
            InitializeComponent();
            _mainWin = mainWin;
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
            try
            {
                byte[] sendData = _sendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf5, new byte[2] { 0, 0 });
                Socket socketGet = GetNearest4GTerminalSocket(true);
                if (socketGet != null)
                {
                    socketGet.Send(sendData, SocketFlags.None);
                }
                else
                {
                    MessageBox.Show(Find4GErrorMsg);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        //private void miGetPointSignalAmplitude_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        if (SocketImport != null)
        //        {
        //            byte[] sendData = _sendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf3, new byte[2] { 0, 0 });
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
            try
            {
                byte[] sendData = _sendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf0, new byte[5] { (byte)_terminalNumber, (byte)NeighbourSmall, (byte)NeighbourBig, (byte)(Is4G ? 1 : 0), (byte)(IsEnd ? 1 : 0) });
                Socket socketGet = GetNearest4GTerminalSocket(true);
                if (socketGet != null)
                {
                    socketGet.Send(sendData, SocketFlags.None);
                }
                else
                {
                    MessageBox.Show(Find4GErrorMsg);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void miReadPointInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainWin != null)
                {
                    _mainWin.WaitingRingEnable();
                    _mainWin.WaitReceiveTimer.Start();
                }
                byte[] sendData = _sendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf1, new byte[1] { (byte)_terminalNumber });
                Socket socketGet = GetNearest4GTerminalSocket(true);
                if (socketGet != null)
                {
                    socketGet.Send(sendData, SocketFlags.None);
                }
                else
                {
                    if (_mainWin != null)
                    {
                        _mainWin.WaitingRingDisable();
                        _mainWin.WaitReceiveTimer.Stop();
                    }
                    MessageBox.Show(Find4GErrorMsg);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void miSignalSendConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SignalSendConfigWindow newSignalSendConfigWin = new SignalSendConfigWindow();

                if (_mainWin != null)
                {
                    newSignalSendConfigWin.Owner = _mainWin;
                }
                if (!newSignalSendConfigWin.ShowDialog().Value)
                {
                    return;
                }
                byte[] sendData = _sendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf4,
                    new byte[4] { (byte)newSignalSendConfigWin.SendInterval,(byte)newSignalSendConfigWin.SendTimeOpportunity,
                                      (byte)newSignalSendConfigWin.NeighbourSmallOpportunity,(byte)newSignalSendConfigWin.NeighbourBigOpportunity  });
                Socket socketGet = GetNearest4GTerminalSocket(true);
                if (socketGet != null)
                {
                    socketGet.Send(sendData, SocketFlags.None);
                }
                else
                {
                    MessageBox.Show(Find4GErrorMsg);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void miThresholdSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThresholdSettingWindow newThresholdSettingWin = new ThresholdSettingWindow();

                if (_mainWin != null)
                {
                    newThresholdSettingWin.Owner = _mainWin;
                }
                if (!newThresholdSettingWin.ShowDialog().Value)
                {
                    return;
                }
                byte[] sendData = _sendDataPackage.PackageSendData(0xff, (byte)_terminalNumber, 0xf2,
                    new byte[2] { (byte)newThresholdSettingWin.ThresholdRail1, (byte)newThresholdSettingWin.ThresholdRail2 });
                Socket socketGet = GetNearest4GTerminalSocket(true);
                if (socketGet != null)
                {
                    socketGet.Send(sendData, SocketFlags.None);
                }
                else
                {
                    MessageBox.Show(Find4GErrorMsg);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
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
