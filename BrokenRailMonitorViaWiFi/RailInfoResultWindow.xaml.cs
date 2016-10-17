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
using System.Xml;
using Visifire.Charts;

namespace BrokenRailMonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for RailInfoResultWindow.xaml
    /// </summary>
    public partial class RailInfoResultWindow : Window
    {
        private string _directoryName;
        private int _terminalNo;
        private List<DateTime> _dateTimeList = new List<DateTime>();
        private int[] _rail1Temprature;
        private int[] _rail2Temprature;
        private int[] _terminalTemprature;
        private int[] _rail1Stress;
        private int[] _rail2Stress;
        private static RailInfoResultWindow _uniqueInstance;
        private MasterControl _masterCtrl;

        public MasterControl MasterCtrl
        {
            get
            {
                return _masterCtrl;
            }

            set
            {
                _masterCtrl = value;
            }
        }

        private RailInfoResultWindow(int terminalNo)
        {
            InitializeComponent();
            this.masterControl.TerminalNumber = terminalNo;
            _terminalNo = terminalNo;
            this.masterControl.HideContextMenu();
        }

        public static RailInfoResultWindow GetInstance(int terminalNo)
        {
            if (_uniqueInstance == null)
            {
                _uniqueInstance = new RailInfoResultWindow(terminalNo);
            }
            return _uniqueInstance;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (MasterCtrl != null)
            {
                if (MasterCtrl.NeighbourSmall == 0)
                {
                    this.rail1Left.Visibility = Visibility.Hidden;
                    this.rail2Left.Visibility = Visibility.Hidden;
                }
                if (MasterCtrl.NeighbourBig == 0xff)
                {
                    this.rail1Right.Visibility = Visibility.Hidden;
                    this.rail2Right.Visibility = Visibility.Hidden;
                }
            }
        }

        public void RefreshResult()
        {
            try
            {
                DateTime now = System.DateTime.Now;
                _directoryName = now.ToString("yyyy") + "\\" + now.ToString("yyyy-MM") + "\\" + now.ToString("yyyy-MM-dd");
                string fileName = System.Environment.CurrentDirectory + @"\DataRecord\" + _directoryName + @"\DataTerminal" + _terminalNo.ToString("D3") + ".xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);
                XmlNodeList xn0 = xmlDoc.SelectSingleNode("Datas").ChildNodes;

                int nodeCount = xn0.Count;
                _rail1Temprature = new int[nodeCount];
                _rail2Temprature = new int[nodeCount];
                _terminalTemprature = new int[nodeCount];
                _rail1Stress = new int[nodeCount];
                _rail2Stress = new int[nodeCount];
                int i = 0;
                foreach (XmlNode node in xn0)
                {
                    //时间
                    XmlNode timeNode = node.SelectSingleNode("Time");
                    string innerTextTime = timeNode.InnerText.Trim();
                    string[] strTime = innerTextTime.Split('-');
                    int[] time = new int[6];
                    for (int j = 0; j < 6; j++)
                    {
                        time[j] = Convert.ToInt32(strTime[j]);
                    }
                    time[0] += 2000;
                    _dateTimeList.Add(new DateTime(time[0], time[1], time[2], time[3], time[4], time[5]));

                    //铁轨1温度
                    XmlNode temprature1Node = node.SelectSingleNode("Rail1/Temprature");
                    string innerText = temprature1Node.InnerText.Trim();
                    string[] twoTemprature = innerText.Split('-');

                    _terminalTemprature[i] = Convert.ToInt32(twoTemprature[0]);
                    int sign = (_terminalTemprature[i] & 0x80) >> 7;
                    if (sign == 1)
                    {
                        _terminalTemprature[i] = -(_terminalTemprature[i] & 0x7f);
                    }
                    _rail1Temprature[i] = Convert.ToInt32(twoTemprature[1]);
                    sign = (_rail1Temprature[i] & 0x80) >> 7;
                    if (sign == 1)
                    {
                        _rail1Temprature[i] = -(_rail1Temprature[i] & 0x7f);
                    }

                    //铁轨2温度
                    XmlNode temprature2Node = node.SelectSingleNode("Rail2/Temprature");
                    string innerText2 = temprature2Node.InnerText.Trim();
                    string[] twoTemprature2 = innerText2.Split('-');
                    _rail2Temprature[i] = Convert.ToInt32(twoTemprature2[1]);
                    sign = (_rail2Temprature[i] & 0x80) >> 7;
                    if (sign == 1)
                    {
                        _rail2Temprature[i] = -(_rail2Temprature[i] & 0x7f);
                    }

                    XmlNode stress1Node = node.SelectSingleNode("Rail1/Stress");
                    string innerTextStress1 = stress1Node.InnerText.Trim();
                    string[] stress1 = innerTextStress1.Split('-');
                    _rail1Stress[i] = (Convert.ToInt32(stress1[0])) << 8 + Convert.ToInt32(stress1[1]);

                    XmlNode stress2Node = node.SelectSingleNode("Rail2/Stress");
                    string innerTextStress2 = stress2Node.InnerText.Trim();
                    string[] stress2 = innerTextStress2.Split('-');
                    _rail2Stress[i] = (Convert.ToInt32(stress2[0])) << 8 + Convert.ToInt32(stress2[1]);

                    //铁轨通断，只看最新的一个记录。
                    if (i == nodeCount - 1)
                    {
                        XmlNode rail1OnOffNode = node.SelectSingleNode("Rail1/OnOff");
                        string rail1OnOffInnerText = rail1OnOffNode.InnerText.Trim();
                        int intRail1OnOff = Convert.ToInt32(rail1OnOffInnerText);
                        //检查1号铁轨
                        int onOffRail1Left = intRail1OnOff & 0x0f;
                        if (onOffRail1Left == 0)
                        {//通的
                            this.rail1Left.Normal();
                        }
                        else if (onOffRail1Left == 7)
                        {//断的
                            this.rail1Left.Error();
                        }
                        else
                        {
                            MessageBox.Show("解析出未定义数据！");
                        }
                        int onOffRail1Right = (intRail1OnOff & 0xf0) >> 4;
                        if (onOffRail1Right == 0)
                        {//通的
                            this.rail1Right.Normal();
                        }
                        else if (onOffRail1Right == 7)
                        {//断的
                            this.rail1Right.Error();
                        }
                        else
                        {
                            MessageBox.Show("解析出未定义数据！");
                        }
                        XmlNode rail2OnOffNode = node.SelectSingleNode("Rail2/OnOff");
                        string rail2OnOffInnerText = rail2OnOffNode.InnerText.Trim();
                        int intRail2OnOff = Convert.ToInt32(rail2OnOffInnerText);
                        //检查2号铁轨
                        int onOffRail2Left = intRail2OnOff & 0x0f;
                        if (onOffRail2Left == 0)
                        {//通的
                            this.rail2Left.Normal();
                        }
                        else if (onOffRail2Left == 7)
                        {//断的
                            this.rail2Left.Error();
                        }
                        else
                        {
                            MessageBox.Show("解析出未定义数据！");
                        }
                        int onOffRail2Right = (intRail2OnOff & 0xf0) >> 4;
                        if (onOffRail2Right == 0)
                        {//通的
                            this.rail2Right.Normal();
                        }
                        else if (onOffRail2Right == 7)
                        {//断的
                            this.rail2Right.Error();
                        }
                        else
                        {
                            MessageBox.Show("解析出未定义数据！");
                        }
                    }
                    i++;
                }

                chartRail1Temprature.Series.Clear();
                DataSeries dataSeries = new DataSeries();
                dataSeries.RenderAs = RenderAs.Line;
                dataSeries.XValueType = ChartValueTypes.DateTime;
                DataPoint dataPoint;
                for (int k = 0; k < nodeCount; k++)
                {
                    dataPoint = new DataPoint();
                    dataPoint.XValue = _dateTimeList[k];
                    dataPoint.YValue = _rail1Temprature[k];
                    dataPoint.MarkerSize = 8;
                    dataSeries.DataPoints.Add(dataPoint);
                }
                chartRail1Temprature.Series.Add(dataSeries);

                chartTerminalTemprature.Series.Clear();
                dataSeries = new DataSeries();
                dataSeries.RenderAs = RenderAs.Line;
                dataSeries.XValueType = ChartValueTypes.DateTime;
                DataPoint dataPoint1;
                for (int k = 0; k < nodeCount; k++)
                {
                    dataPoint1 = new DataPoint();
                    dataPoint1.XValue = _dateTimeList[k];
                    dataPoint1.YValue = _terminalTemprature[k];
                    dataPoint1.MarkerSize = 8;
                    dataSeries.DataPoints.Add(dataPoint1);
                }
                chartTerminalTemprature.Series.Add(dataSeries);

                chartRail2Temprature.Series.Clear();
                dataSeries = new DataSeries();
                dataSeries.RenderAs = RenderAs.Line;
                dataSeries.XValueType = ChartValueTypes.DateTime;
                DataPoint dataPoint2;
                for (int k = 0; k < nodeCount; k++)
                {
                    dataPoint2 = new DataPoint();
                    dataPoint2.XValue = _dateTimeList[k];
                    dataPoint2.YValue = _rail2Temprature[k];
                    dataPoint2.MarkerSize = 8;
                    dataSeries.DataPoints.Add(dataPoint2);
                }
                chartRail2Temprature.Series.Add(dataSeries);

                chartRail1Stress.Series.Clear();
                dataSeries = new DataSeries();
                dataSeries.RenderAs = RenderAs.Line;
                dataSeries.XValueType = ChartValueTypes.DateTime;
                DataPoint dataPointStress1;
                for (int k = 0; k < nodeCount; k++)
                {
                    dataPointStress1 = new DataPoint();
                    dataPointStress1.XValue = _dateTimeList[k];
                    dataPointStress1.YValue = _rail1Stress[k];
                    dataPointStress1.MarkerSize = 8;
                    dataSeries.DataPoints.Add(dataPointStress1);
                }
                chartRail1Stress.Series.Add(dataSeries);

                chartRail2Stress.Series.Clear();
                dataSeries = new DataSeries();
                dataSeries.RenderAs = RenderAs.Line;
                dataSeries.XValueType = ChartValueTypes.DateTime;
                DataPoint dataPointStress2;
                for (int k = 0; k < nodeCount; k++)
                {
                    dataPointStress2 = new DataPoint();
                    dataPointStress2.XValue = _dateTimeList[k];
                    dataPointStress2.YValue = _rail2Stress[k];
                    dataPointStress2.MarkerSize = 8;
                    dataSeries.DataPoints.Add(dataPointStress2);
                }
                chartRail2Stress.Series.Add(dataSeries);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

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

            //lineChart.HideIndicator();
            //root.Children.RemoveAt(10);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _uniqueInstance = null;
        }
    }
}
