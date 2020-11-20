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
    /// CmdDetailConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CmdDetailConfigWindow : Window
    {
        public int CmdContent { get; set; } = 1;

        public string MyTitle { get; set; }

        public CmdDetailConfigWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            MyTitle = "详细信息配置";
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            byte[] content = new byte[1];
            content[0] = (byte)(CmdContent & 0xff);
            byte[] sendData = SendDataPackage.PackageSendData(Command3Type.SendCmd, ConfigType.SET_AmpConfig, 1, content);
            MainWindow.GetInstance().SendCommand(sendData);
        }
    }
}
