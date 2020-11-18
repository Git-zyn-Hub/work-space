﻿using System;
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
    /// ConfigInfoUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigInfoUserControl : UserControl
    {
        public ConfigInfoUserControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public int 终端编号 { get; set; }

        public int 当前系统时间 { get; set; }
        public int CPU型号 { get; set; }
        public int FPGA型号 { get; set; }
        public int 主频 { get; set; }
        public int CPU固件版本 { get; set; }
        public int FPGA固件版本 { get; set; }
        public int 存储空间合计 { get; set; }
        public int 存储空间剩余 { get; set; }
        public int 发送超声波频率 { get; set; }
        public int 发送超声波长度 { get; set; }
        public int 接收判断门限 { get; set; }
        public int 测试间隔 { get; set; }
        public int 信息上报频率 { get; set; }
        public int 系统重启次数 { get; set; }
        public int 上次重启时间 { get; set; }
        public int 连续工作时间长度 { get; set; }
        public int 详细信息的编号 { get; set; }
        public int 对时间隔 { get; set; }
        public double 电压12V { get; set; }
        public double 电压5V { get; set; }
        public double 电压3_3V { get; set; }
        public double 电压1_5V { get; set; }
        public double 电流12V { get; set; }
    }
}