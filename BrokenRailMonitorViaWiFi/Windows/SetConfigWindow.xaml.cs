using BrokenRail3MonitorViaWiFi.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BrokenRail3MonitorViaWiFi.Windows
{
    /// <summary>
    /// SetConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SetConfigWindow : Window, INotifyPropertyChanged
    {
        public string MyTitle { get; set; }
        public int 终端编号 { get; set; } = 1;
        public int 发送超声波频率 { get; set; } = 25;
        public int 发送超声波长度 { get; set; } = 250;
        public int 接收判断门限 { get; set; } = 0;
        public int 信息上报频率 { get; set; } = 5;
        public int 详细信息的编号 { get; set; } = 1;
        public int 对时间隔 { get; set; } = 6;

        public SetConfigWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            MyTitle = "下发配置";
            ConfigInfoWindow winConfigInfo = MainWindow.GetInstance().GetConfigInfoWin();
            if (winConfigInfo != null && winConfigInfo.ucConfigInfo != null)
            {
                终端编号 = winConfigInfo.ucConfigInfo.终端编号;
                发送超声波频率 = winConfigInfo.ucConfigInfo.发送超声波频率;
                发送超声波长度 = winConfigInfo.ucConfigInfo.发送超声波长度;
                接收判断门限 = winConfigInfo.ucConfigInfo.接收判断门限;
                信息上报频率 = winConfigInfo.ucConfigInfo.信息上报频率;
                详细信息的编号 = winConfigInfo.ucConfigInfo.详细信息的编号;
                对时间隔 = winConfigInfo.ucConfigInfo.对时间隔;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            byte[] content = new byte[46];
            content[0] = (byte)终端编号;
            content[18] = (byte)发送超声波频率;
            byte[] sendLength = CalcIntFromBytes.Calc2BytesFromInt(发送超声波长度);
            content[19] = sendLength[0];
            content[20] = sendLength[1];
            content[21] = (byte)接收判断门限;
            content[23] = (byte)信息上报频率;
            content[34] = (byte)详细信息的编号;
            content[35] = (byte)对时间隔;

            byte[] sendData = SendDataPackage.PackageSendData(Command3Type.SendCmd, ConfigType.SET_CONFIG, 46, content);
            MainWindow.GetInstance().SendCommand(sendData);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void NotifyChanged()
        {
            OnPropertyChanged("MyTitle");
            OnPropertyChanged("终端编号");
            OnPropertyChanged("发送超声波频率");
            OnPropertyChanged("发送超声波长度");
            OnPropertyChanged("接收判断门限");
            OnPropertyChanged("信息上报频率");
            OnPropertyChanged("详细信息的编号");
            OnPropertyChanged("对时间隔");
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

        private void winSetConfig_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyChanged();
        }
    }
}
