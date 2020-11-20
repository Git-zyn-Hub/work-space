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
using Visifire.Charts;

namespace BrokenRail3MonitorViaWiFi.UserControls
{
    /// <summary>
    /// FFTChartUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class FFTChartUserControl : UserControl
    {
        public FFTChartUserControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public int InfoNumber { get; set; }

        public byte[] Data { get; set; }
        private void lineChart_Rendered(object sender, EventArgs e)
        {
            var c = sender as Chart;
            var legend = c.Legends[0];
            var root = legend.Parent as Grid;
            //移除水印
            if (root != null)
            {
                root.Children.RemoveAt(9);
            }
        }

        public void Refresh()
        {
            if (ckbShowDC.IsChecked.HasValue && !ckbShowDC.IsChecked.Value)
            {
                UpdateChart(4);
            }
            else
            {
                UpdateChart(0);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateChart(0);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateChart(4);
        }

        /// <summary>
        /// 更新图表
        /// </summary>
        /// <param name="startIndex">起始字节索引，如果为0则包含直流分量，为4则不包含</param>
        private void UpdateChart(int startIndex)
        {
            if (Data == null || Data.Length != 512)
            {
                return;
            }
            chartFFT.Series.Clear();
            DataSeries fftSeries = new DataSeries();
            fftSeries.RenderAs = RenderAs.Column;
            fftSeries.MarkerEnabled = false;
            fftSeries.XValueType = ChartValueTypes.Numeric;
            fftSeries.LegendText = "信号";
            DataPoint dpFFTPoint;
            for (int k = startIndex; k < 512; k += 4)
            {
                dpFFTPoint = new DataPoint();
                dpFFTPoint.XValue = k / 4;
                dpFFTPoint.YValue = CalcIntFromBytes.CalcIntFrom4Bytes(Data, k);
                fftSeries.DataPoints.Add(dpFFTPoint);
            }
            chartFFT.Series.Add(fftSeries);
        }
    }
}
