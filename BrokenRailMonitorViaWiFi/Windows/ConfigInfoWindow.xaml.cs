using BrokenRail3MonitorViaWiFi;
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

namespace BrokenRail3MonitorViaWiFi.Windows
{
    /// <summary>
    /// RealtimeInfoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigInfoWindow : Window
    {
        public ConfigInfoWindow()
        {
            InitializeComponent();
        }

        public void SetData(byte[] data)
        {
            int length = (data[33] << 8) + data[34];
            if (length == 46)
            {
                ucConfigInfo.终端编号 = data[35];
                ucConfigInfo.当前系统时间 = CalcIntFromBytes.CalcIntFrom4Bytes(data, 36);
                ucConfigInfo.CPU型号 = data[40];
                ucConfigInfo.FPGA型号 = data[41];
                ucConfigInfo.主频 = data[42];
                ucConfigInfo.CPU固件版本 = data[43];
                ucConfigInfo.FPGA固件版本 = data[44];
                ucConfigInfo.存储空间合计 = CalcIntFromBytes.CalcIntFrom4Bytes(data, 45);
                ucConfigInfo.存储空间剩余 = CalcIntFromBytes.CalcIntFrom4Bytes(data, 49);
                ucConfigInfo.发送超声波频率 = data[53];
                ucConfigInfo.发送超声波长度 = CalcIntFromBytes.CalcIntFrom2Bytes(data, 54);
                ucConfigInfo.接收判断门限 = data[56];
                ucConfigInfo.测试间隔 = data[57];
                ucConfigInfo.信息上报频率 = data[58];
                ucConfigInfo.系统重启次数 = CalcIntFromBytes.CalcIntFrom2Bytes(data, 59);
                ucConfigInfo.上次重启时间 = CalcIntFromBytes.CalcIntFrom4Bytes(data, 61);
                ucConfigInfo.连续工作时间长度 = CalcIntFromBytes.CalcIntFrom4Bytes(data, 65);
                ucConfigInfo.详细信息的编号 = data[69];
                ucConfigInfo.对时间隔 = data[70];
                ucConfigInfo.电压12V = CalcIntFromBytes.CalcIntFrom2Bytes(data, 71) / 100.0;
                ucConfigInfo.电压5V = CalcIntFromBytes.CalcIntFrom2Bytes(data, 73) / 100.0;
                ucConfigInfo.电压3_3V = CalcIntFromBytes.CalcIntFrom2Bytes(data, 75) / 100.0;
                ucConfigInfo.电压1_5V = CalcIntFromBytes.CalcIntFrom2Bytes(data, 77) / 100.0;
                ucConfigInfo.电流12V = CalcIntFromBytes.CalcIntFrom2Bytes(data, 79);
            }
            else
            {
                MainWindow.GetInstance().AppendMessage("配置信息长度出错", DataLevel.Error);
            }
        }
    }
}
