using BrokenRail3MonitorViaWiFi;
using System;
using System.Collections.Generic;
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
    /// SendCmdWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CmdTNumWindow : Window
    {
        public int CmdContent { get; set; } = 1;

        public string MyTitle { get; set; }

        public CmdTNumWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            MyTitle = "设置终端号";
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            byte[] content = new byte[2];
            content[0] = (byte)((CmdContent & 0xff00) >> 8);
            content[1] = (byte)(CmdContent & 0xff);
            byte[] sendData = SendDataPackage.PackageSendData(Command3Type.SendCmd, ConfigType.SET_TNum, 2, content);
            MainWindow.GetInstance().SendCommand(sendData);
        }
    }
}
