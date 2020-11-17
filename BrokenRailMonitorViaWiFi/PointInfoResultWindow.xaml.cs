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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BrokenRail3MonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for PointInfoResultWindow.xaml
    /// </summary>
    public partial class PointInfoResultWindow : Window
    {
        private int _terminalNo;
        private int _neighbourSmall;
        private int _neighbourBig;
        private bool _is4G;
        private bool _isEnd;
        public PointInfoResultWindow(int terminalNo, int neighbourSmall, int neighbourBig, bool is4G, bool isEnd)
        {
            InitializeComponent();
            _terminalNo = terminalNo;
            _neighbourSmall = neighbourSmall;
            _neighbourBig = neighbourBig;
            _is4G = is4G;
            _isEnd = isEnd;
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.lblTerminalNo.Content = _terminalNo.ToString() + "号终端";
            this.lblNeighbourSmall.Content = _neighbourSmall.ToString();
            this.lblNeighbourBig.Content = _neighbourBig.ToString();
            this.lblIs4G.Content = _is4G ? "是" : "否";
            this.lblIsEnd.Content = _isEnd ? "是" : "否";
        }
    }
}
