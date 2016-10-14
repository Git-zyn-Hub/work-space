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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BrokenRailMonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for DataShowUserControl.xaml
    /// </summary>
    public partial class DataShowUserControl : UserControl
    {
        public DataShowUserControl()
        {
            InitializeComponent();
        }

        public void AddShowData(string data, DataLevel dataLevel)
        {
            string nowTime = System.DateTime.Now.ToString("HH:mm:ss");
            TextBlock txtData = new TextBlock();
            switch (dataLevel)
            {
                case DataLevel.Default:
                    txtData.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case DataLevel.Normal:
                    txtData.Foreground = new SolidColorBrush(Colors.LightGreen);
                    break;
                case DataLevel.Warning:
                    txtData.Foreground = new SolidColorBrush(Colors.Orange);
                    txtData.FontWeight = FontWeights.Bold;
                    break;
                case DataLevel.Error:
                    txtData.Foreground = new SolidColorBrush(Colors.Red);
                    break;
                default:
                    txtData.Foreground = new SolidColorBrush(Colors.Black);
                    break;
            }
            txtData.Text = nowTime + "  " + data;
            this.stpContainer.Children.Add(txtData);
            this.scrollViewer.Focus();
            this.scrollViewer.ScrollToEnd();
        }
    }

    public enum DataLevel
    {
        Default,
        Normal,
        Warning,
        Error
    }
}
