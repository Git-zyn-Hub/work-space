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

namespace BrokenRailMonitorViaWiFi.Windows
{
    /// <summary>
    /// Interaction logic for GetHistoryWindow.xaml
    /// </summary>
    public partial class GetHistoryWindow : Window
    {
        public int YearStart { get; set; }
        public int MonthStart { get; set; }
        public int DayStart { get; set; }
        public int HourStart { get; set; }
        public int MinuteStart { get; set; }
        public int SecondStart { get; set; }
        public int YearEnd { get; set; }
        public int MonthEnd { get; set; }
        public int DayEnd { get; set; }
        public int HourEnd { get; set; }
        public int MinuteEnd { get; set; }
        public int SecondEnd { get; set; }
        public GetHistoryWindow()
        {
            InitializeComponent();
        }
        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            if (this.dpStartDate.SelectedDate.HasValue)
            {
                this.YearStart = this.dpStartDate.SelectedDate.Value.Year % 100;//年只有一字节，不算2000，如2016只记做16.
                this.MonthStart = this.dpStartDate.SelectedDate.Value.Month;
                this.DayStart = this.dpStartDate.SelectedDate.Value.Day;
            }
            else
            {
                MessageBox.Show("‘开始日期’必须选择一个值！");
                return;
            }
            if (this.tpStartTime.SelectedTime.HasValue)
            {
                this.HourStart = this.tpStartTime.SelectedTime.Value.Hour;
                this.MinuteStart = this.tpStartTime.SelectedTime.Value.Minute;
                this.SecondStart = this.tpStartTime.SelectedTime.Value.Second;
            }
            if (this.dpEndDate.SelectedDate.HasValue)
            {
                this.YearEnd = this.dpEndDate.SelectedDate.Value.Year % 100;//年只有一字节，不算2000，如2016只记做16.
                this.MonthEnd = this.dpEndDate.SelectedDate.Value.Month;
                this.DayEnd = this.dpEndDate.SelectedDate.Value.Day;
            }
            else
            {
                MessageBox.Show("‘结束日期’必须选择一个值！");
                return;
            }
            if (this.tpEndTime.SelectedTime.HasValue)
            {
                this.HourEnd = this.tpEndTime.SelectedTime.Value.Hour;
                this.MinuteEnd = this.tpEndTime.SelectedTime.Value.Minute;
                this.SecondEnd = this.tpEndTime.SelectedTime.Value.Second;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
