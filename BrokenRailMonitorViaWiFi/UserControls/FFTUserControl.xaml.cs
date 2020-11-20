using BrokenRail3MonitorViaWiFi.Classes;
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

namespace BrokenRail3MonitorViaWiFi.UserControls
{
    /// <summary>
    /// FFTUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class FFTUserControl : UserControl
    {
        private static readonly int CHART_COUNT = 8;
        private FFTChartUserControl[] _fftCharts = new FFTChartUserControl[CHART_COUNT];
        public FFTUserControl(byte[] data)
        {
            InitializeComponent();
            for (int i = 0; i < CHART_COUNT; i++)
            {
                _fftCharts[i] = new FFTChartUserControl();
                _fftCharts[i].InfoNumber = i + 1;
                byte[] oneData = new byte[512];
                Array.Copy(data, i * 512 + 14, oneData, 0, 512);
                _fftCharts[i].Data = oneData;
                stpContainer.Children.Add(_fftCharts[i]);
                SetTime(data);
            }
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < CHART_COUNT; i++)
            {
                _fftCharts[i].ckbShowDC.IsChecked = true;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < CHART_COUNT; i++)
            {
                _fftCharts[i].ckbShowDC.IsChecked = false;
            }
        }

        public void SetData(byte[] data)
        {
            for (int i = 0; i < CHART_COUNT; i++)
            {
                byte[] oneData = new byte[512];
                Array.Copy(data, i * 512 + 14, oneData, 0, 512);
                _fftCharts[i].Data = oneData;
                _fftCharts[i].Refresh();
                SetTime(data);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ckbShowDC.IsChecked = true;
        }

        private void SetTime(byte[] data)
        {
            lblTime.Content = TimeStamp.GetDateTime(CalcIntFromBytes.CalcIntFrom4Bytes(data, 5)).ToString("yyyy/MM/dd HH:mm:ss");
        }
    }
}
