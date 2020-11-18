using BrokenRail3MonitorViaWiFi.Classes;
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
using System.Windows.Shapes;
using Visifire.Charts;
using Visifire.Commons;

namespace BrokenRail3MonitorViaWiFi.Windows
{
    /// <summary>
    /// TongDuanWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TongDuanWindow : Window
    {
        public TongDuanWindow()
        {
            InitializeComponent();
        }

        private void lineChart_Rendered(object sender, EventArgs e)
        {
            var c = sender as Chart;
            var legend = c.Legends[0];
            var root = legend.Parent as Grid;
            //移除水印
            if (root != null)
            {
                root.Children.RemoveAt(10);
            }

            //lineChart.HideIndicator();
        }

        public void SetData(byte[] data)
        {
            int length = CalcIntFromBytes.CalcIntFrom2Bytes(data, 3);
            if (length == 153)
            {
                lblTime.Content = TimeStamp.GetDateTime(CalcIntFromBytes.CalcIntFrom4Bytes(data, 5)).ToString();
                lblTemperature.Content = CalcIntFromBytes.CalcIntFrom2Bytes(data, 12);
                masterControl.TerminalNumber = data[11];
                SetRailsStatus(data[18]);
                RefreshCharts(data, 21);
            }
        }

        private void SetOneRailStatus(Rail rail, byte tongDuan, int mask)
        {
            if ((tongDuan & mask) == mask)
            {
                rail.Normal();
            }
            else
            {
                rail.Error();
            }
        }

        private void SetRailsStatus(byte tongDuan)
        {
            SetOneRailStatus(railLL, tongDuan, 0x08);
            SetOneRailStatus(railLR, tongDuan, 0x04);
            SetOneRailStatus(railRL, tongDuan, 0x02);
            SetOneRailStatus(railRR, tongDuan, 0x01);
        }

        public void RefreshCharts(byte[] data, int index)
        {
            RefreshOneChart(chartAmpRailLUp, data, index);
            RefreshOneChart(chartAmpRailLDown, data, index + 28);
            RefreshOneChart(chartAmpRailRUp, data, index + 56);
            RefreshOneChart(chartAmpRailRDown, data, index + 84);
        }
        private void RefreshOneChart(Chart chart, byte[] data, int index)
        {
            chart.Series.Clear();
            DataSeries signalSeries = new DataSeries();
            signalSeries.RenderAs = RenderAs.Column;
            signalSeries.MarkerEnabled = false;
            signalSeries.XValueType = ChartValueTypes.Numeric;
            signalSeries.ToolTipText = "时间标#XValue，幅度#YValue，SNR=#ZValue";
            signalSeries.LegendText = "信号";
            DataPoint dpSignalAmplitude;
            for (int k = 0; k < 4; k++)
            {
                dpSignalAmplitude = new DataPoint();
                dpSignalAmplitude.XValue = CalcIntFromBytes.CalcIntFrom2Bytes(data, index + k * 2) / 1000.0;
                dpSignalAmplitude.YValue = CalcIntFromBytes.CalcIntFrom4Bytes(data, index + 8 + k * 4);
                int noise = CalcIntFromBytes.CalcIntFrom4Bytes(data, index + 24);
                dpSignalAmplitude.ZValue = noise == 0 ? 0 : CalcIntFromBytes.CalcIntFrom4Bytes(data, index + 8 + k * 4) / noise;
                signalSeries.DataPoints.Add(dpSignalAmplitude);
            }
            DataSeries noiseSeries = new DataSeries();
            noiseSeries.RenderAs = RenderAs.Column;
            noiseSeries.MarkerEnabled = false;
            noiseSeries.XValueType = ChartValueTypes.Numeric;
            noiseSeries.LegendText = "噪声";
            DataPoint dpNoistAmplitude;
            dpNoistAmplitude = new DataPoint();
            dpNoistAmplitude.XValue = 30;
            dpNoistAmplitude.YValue = CalcIntFromBytes.CalcIntFrom4Bytes(data, index + 24);
            noiseSeries.DataPoints.Add(dpNoistAmplitude);
            chart.Series.Add(signalSeries);
            chart.Series.Add(noiseSeries);
        }
    }
}
