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
using Visifire.Commons;

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
        private int[] _rail1ThisAmplitude;
        private int[] _rail2ThisAmplitude;
        private int[] _rail1Stress;
        private int[] _rail2Stress;
        private List<int[]> _rail1LeftSigAmpList;
        private List<int[]> _rail1RightSigAmpList;
        private List<int[]> _rail2LeftSigAmpList;
        private List<int[]> _rail2RightSigAmpList;
        private static RailInfoResultWindow _uniqueInstance;
        private MasterControl _masterCtrl;
        private List<CheckBox> _checkBoxes = new List<CheckBox>();
        private FullScreenChartWindow _fullScreenWin;
        private List<Chart> _charts = new List<Chart>();
        private double _originChartWidth = 0;
        private double _originChartHeight = 0;
        private int _checkCount = 0;
        private string _fileName;

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

        public string FileName
        {
            get
            {
                return _fileName;
            }

            set
            {
                _fileName = value;
            }
        }

        private RailInfoResultWindow(int terminalNo)
        {
            InitializeComponent();
            this.masterControl.TerminalNumber = terminalNo;
            _terminalNo = terminalNo;
            DateTime now = System.DateTime.Now;
            _directoryName = now.ToString("yyyy") + "\\" + now.ToString("yyyy-MM") + "\\" + now.ToString("yyyy-MM-dd");
            FileName = System.Environment.CurrentDirectory + @"\DataRecord\" + _directoryName + @"\DataTerminal" + _terminalNo.ToString("D3") + ".xml";
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
            for (int i = 0; i < 9; i++)
            {
                CheckBox aCheckBox = new CheckBox();
                aCheckBox.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Right);
                aCheckBox.SetValue(VerticalAlignmentProperty, VerticalAlignment.Top);
                aCheckBox.SetValue(Grid.RowProperty, i / 3);
                aCheckBox.SetValue(Grid.ColumnProperty, i % 3);
                aCheckBox.SetValue(Panel.ZIndexProperty, 100);
                aCheckBox.Checked += ACheckBox_Checked;
                aCheckBox.Unchecked += ACheckBox_Unchecked;
                this.gridChart.Children.Add(aCheckBox);
                this._checkBoxes.Add(aCheckBox);
            }
        }

        private void ACheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _checkCount--;
        }

        private void ACheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _checkCount++;
            if (_checkCount > 4)
            {
                CheckBox cbSender = sender as CheckBox;
                if (cbSender != null)
                {
                    cbSender.IsChecked = false;
                    MessageBox.Show("最多只能选择4个图表进行全屏！");
                }
            }
        }

        public void RefreshResult()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(FileName);
                XmlNodeList xn0 = xmlDoc.SelectSingleNode("Datas").ChildNodes;
                drawChart(xn0);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void drawChart(XmlNodeList xnl)
        {

            int nodeCount = xnl.Count;
            this.txtDataCount.Text = nodeCount.ToString();
            _dateTimeList.Clear();
            _rail1Temprature = new int[nodeCount];
            _rail2Temprature = new int[nodeCount];
            _terminalTemprature = new int[nodeCount];
            _rail1ThisAmplitude = new int[nodeCount];
            _rail2ThisAmplitude = new int[nodeCount];
            _rail1Stress = new int[nodeCount];
            _rail2Stress = new int[nodeCount];
            _rail1LeftSigAmpList = new List<int[]>();
            _rail1RightSigAmpList = new List<int[]>();
            _rail2LeftSigAmpList = new List<int[]>();
            _rail2RightSigAmpList = new List<int[]>();
            int i = 0;
            foreach (XmlNode node in xnl)
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

                XmlNode rail1ThisAmpNode = node.SelectSingleNode("Rail1/ThisAmplitude");
                string innerTextRail1ThisAmp = rail1ThisAmpNode.InnerText.Trim();
                string[] rail1ThisAmp = innerTextRail1ThisAmp.Split('-');
                _rail1ThisAmplitude[i] = ((Convert.ToInt32(rail1ThisAmp[0])) << 8) + Convert.ToInt32(rail1ThisAmp[1]);

                XmlNode rail2ThisAmpNode = node.SelectSingleNode("Rail2/ThisAmplitude");
                string innerTextRail2ThisAmp = rail2ThisAmpNode.InnerText.Trim();
                string[] rail2ThisAmp = innerTextRail2ThisAmp.Split('-');
                _rail2ThisAmplitude[i] = ((Convert.ToInt32(rail2ThisAmp[0])) << 8) + Convert.ToInt32(rail2ThisAmp[1]);

                XmlNode stress1Node = node.SelectSingleNode("Rail1/Stress");
                string innerTextStress1 = stress1Node.InnerText.Trim();
                string[] stress1 = innerTextStress1.Split('-');
                _rail1Stress[i] = ((Convert.ToInt32(stress1[0])) << 8) + Convert.ToInt32(stress1[1]);

                XmlNode stress2Node = node.SelectSingleNode("Rail2/Stress");
                string innerTextStress2 = stress2Node.InnerText.Trim();
                string[] stress2 = innerTextStress2.Split('-');
                _rail2Stress[i] = ((Convert.ToInt32(stress2[0])) << 8) + Convert.ToInt32(stress2[1]);

                XmlNode rail1LeftSigAmpNode = node.SelectSingleNode("Rail1/SignalAmplitudeLeft");
                string innerTextRail1LeftSigAmp = rail1LeftSigAmpNode.InnerText.Trim();
                string[] strRail1LeftSigAmp = innerTextRail1LeftSigAmp.Split('-');
                int[] intRail1LeftSigAmp = new int[4];
                for (int k = 0; k < strRail1LeftSigAmp.Length; k += 4)
                {
                    intRail1LeftSigAmp[k / 4] = (Convert.ToInt32(strRail1LeftSigAmp[k]) << 24) +
                                            (Convert.ToInt32(strRail1LeftSigAmp[k + 1]) << 16) +
                                            (Convert.ToInt32(strRail1LeftSigAmp[k + 2]) << 8) +
                                            (Convert.ToInt32(strRail1LeftSigAmp[k + 3]));
                }
                _rail1LeftSigAmpList.Add(intRail1LeftSigAmp);

                XmlNode rail1RightSigAmpNode = node.SelectSingleNode("Rail1/SignalAmplitudeRight");
                string innerTextRail1RightSigAmp = rail1RightSigAmpNode.InnerText.Trim();
                string[] strRail1RightSigAmp = innerTextRail1RightSigAmp.Split('-');
                int[] intRail1RightSigAmp = new int[4];
                for (int k = 0; k < strRail1RightSigAmp.Length; k += 4)
                {
                    intRail1RightSigAmp[k / 4] = (Convert.ToInt32(strRail1RightSigAmp[k]) << 24) +
                                            (Convert.ToInt32(strRail1RightSigAmp[k + 1]) << 16) +
                                            (Convert.ToInt32(strRail1RightSigAmp[k + 2]) << 8) +
                                            (Convert.ToInt32(strRail1RightSigAmp[k + 3]));
                }
                _rail1RightSigAmpList.Add(intRail1RightSigAmp);

                XmlNode rail2LeftSigAmpNode = node.SelectSingleNode("Rail2/SignalAmplitudeLeft");
                string innerTextRail2LeftSigAmp = rail2LeftSigAmpNode.InnerText.Trim();
                string[] strRail2LeftSigAmp = innerTextRail2LeftSigAmp.Split('-');
                int[] intRail2LeftSigAmp = new int[4];
                for (int k = 0; k < strRail2LeftSigAmp.Length; k += 4)
                {
                    intRail2LeftSigAmp[k / 4] = (Convert.ToInt32(strRail2LeftSigAmp[k]) << 24) +
                                            (Convert.ToInt32(strRail2LeftSigAmp[k + 1]) << 16) +
                                            (Convert.ToInt32(strRail2LeftSigAmp[k + 2]) << 8) +
                                            (Convert.ToInt32(strRail2LeftSigAmp[k + 3]));
                }
                _rail2LeftSigAmpList.Add(intRail2LeftSigAmp);

                XmlNode rail2RightSigAmpNode = node.SelectSingleNode("Rail2/SignalAmplitudeRight");
                string innerTextRail2RightSigAmp = rail2RightSigAmpNode.InnerText.Trim();
                string[] strRail2RightSigAmp = innerTextRail2RightSigAmp.Split('-');
                int[] intRail2RightSigAmp = new int[4];
                for (int k = 0; k < strRail2RightSigAmp.Length; k += 4)
                {
                    intRail2RightSigAmp[k / 4] = (Convert.ToInt32(strRail2RightSigAmp[k]) << 24) +
                                            (Convert.ToInt32(strRail2RightSigAmp[k + 1]) << 16) +
                                            (Convert.ToInt32(strRail2RightSigAmp[k + 2]) << 8) +
                                            (Convert.ToInt32(strRail2RightSigAmp[k + 3]));
                }
                _rail2RightSigAmpList.Add(intRail2RightSigAmp);

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

            chartTemprature.Series.Clear();
            DataSeries dataSeries = new DataSeries();
            dataSeries.LegendText = "轨1温度";
            dataSeries.RenderAs = RenderAs.Line;
            dataSeries.XValueType = ChartValueTypes.DateTime;
            DataPoint dataPoint;
            for (int k = 0; k < nodeCount; k++)
            {
                dataPoint = new DataPoint();
                dataPoint.XValue = _dateTimeList[k];
                dataPoint.YValue = _rail1Temprature[k];
                dataPoint.MarkerEnabled = false;
                dataSeries.DataPoints.Add(dataPoint);

            }
            chartTemprature.Series.Add(dataSeries);

            dataSeries = new DataSeries();
            dataSeries.LegendText = "终端温度";
            dataSeries.RenderAs = RenderAs.Line;
            dataSeries.XValueType = ChartValueTypes.DateTime;
            DataPoint dataPoint1;
            for (int k = 0; k < nodeCount; k++)
            {
                dataPoint1 = new DataPoint();
                dataPoint1.XValue = _dateTimeList[k];
                dataPoint1.YValue = _terminalTemprature[k];
                dataPoint1.MarkerEnabled = false;
                dataSeries.DataPoints.Add(dataPoint1);
            }
            chartTemprature.Series.Add(dataSeries);

            dataSeries = new DataSeries();
            dataSeries.LegendText = "轨2温度";
            dataSeries.RenderAs = RenderAs.Line;
            dataSeries.XValueType = ChartValueTypes.DateTime;
            DataPoint dataPoint2;
            for (int k = 0; k < nodeCount; k++)
            {
                dataPoint2 = new DataPoint();
                dataPoint2.XValue = _dateTimeList[k];
                dataPoint2.YValue = _rail2Temprature[k];
                dataPoint2.MarkerEnabled = false;
                dataSeries.DataPoints.Add(dataPoint2);
            }
            chartTemprature.Series.Add(dataSeries);

            chartRail1ThisAmplitude.Series.Clear();
            dataSeries = new DataSeries();
            dataSeries.RenderAs = RenderAs.Line;
            dataSeries.MarkerEnabled = false;
            dataSeries.XValueType = ChartValueTypes.DateTime;
            DataPoint dpRail1ThisAmplitude;
            for (int k = 0; k < nodeCount; k++)
            {
                dpRail1ThisAmplitude = new DataPoint();
                dpRail1ThisAmplitude.XValue = _dateTimeList[k];
                dpRail1ThisAmplitude.YValue = _rail1ThisAmplitude[k];
                dataSeries.DataPoints.Add(dpRail1ThisAmplitude);
            }
            chartRail1ThisAmplitude.Series.Add(dataSeries);

            chartRail2ThisAmplitude.Series.Clear();
            dataSeries = new DataSeries();
            dataSeries.RenderAs = RenderAs.Line;
            dataSeries.MarkerEnabled = false;
            dataSeries.XValueType = ChartValueTypes.DateTime;
            DataPoint dpRail2ThisAmplitude;
            for (int k = 0; k < nodeCount; k++)
            {
                dpRail2ThisAmplitude = new DataPoint();
                dpRail2ThisAmplitude.XValue = _dateTimeList[k];
                dpRail2ThisAmplitude.YValue = _rail2ThisAmplitude[k];
                dataSeries.DataPoints.Add(dpRail2ThisAmplitude);
            }
            chartRail2ThisAmplitude.Series.Add(dataSeries);

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
                dataPointStress1.MarkerEnabled = false;
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
                dataPointStress2.MarkerEnabled = false;
                dataSeries.DataPoints.Add(dataPointStress2);
            }
            chartRail2Stress.Series.Add(dataSeries);

            chartRail1LeftSignalAmplitude.Series.Clear();
            dataSeries = new DataSeries();
            dataSeries.RenderAs = RenderAs.Line;
            dataSeries.XValueType = ChartValueTypes.DateTime;
            DataPoint dPRail1LeftSigAmp;
            int rail1LeftMax = 0;
            for (int j = 0; j < _rail1LeftSigAmpList.Count; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    dPRail1LeftSigAmp = new DataPoint();
                    dPRail1LeftSigAmp.XValue = _dateTimeList[j].AddSeconds(k);
                    dPRail1LeftSigAmp.YValue = _rail1LeftSigAmpList[j][k];
                    rail1LeftMax = Math.Max(_rail1LeftSigAmpList[j][k], rail1LeftMax);
                    setPointMarker(dPRail1LeftSigAmp, k);
                    dataSeries.DataPoints.Add(dPRail1LeftSigAmp);
                }
            }
            changeYAxisMax(chartRail1LeftSignalAmplitude, rail1LeftMax);
            chartRail1LeftSignalAmplitude.Series.Add(dataSeries);

            chartRail1RightSignalAmplitude.Series.Clear();
            dataSeries = new DataSeries();
            dataSeries.RenderAs = RenderAs.Line;
            dataSeries.XValueType = ChartValueTypes.DateTime;
            DataPoint dPRail1RightSigAmp;
            int rail1RightMax = 0;
            for (int j = 0; j < _rail1RightSigAmpList.Count; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    dPRail1RightSigAmp = new DataPoint();
                    dPRail1RightSigAmp.XValue = _dateTimeList[j].AddSeconds(k);
                    dPRail1RightSigAmp.YValue = _rail1RightSigAmpList[j][k];
                    rail1RightMax = Math.Max(_rail1RightSigAmpList[j][k], rail1RightMax);
                    setPointMarker(dPRail1RightSigAmp, k);
                    dataSeries.DataPoints.Add(dPRail1RightSigAmp);
                }
            }
            changeYAxisMax(chartRail1RightSignalAmplitude, rail1RightMax);
            chartRail1RightSignalAmplitude.Series.Add(dataSeries);

            chartRail2LeftSignalAmplitude.Series.Clear();
            dataSeries = new DataSeries();
            dataSeries.RenderAs = RenderAs.Line;
            dataSeries.XValueType = ChartValueTypes.DateTime;
            DataPoint dPRail2LeftSigAmp;
            int rail2LeftMax = 0;
            for (int j = 0; j < _rail2LeftSigAmpList.Count; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    dPRail2LeftSigAmp = new DataPoint();
                    dPRail2LeftSigAmp.XValue = _dateTimeList[j].AddSeconds(k);
                    dPRail2LeftSigAmp.YValue = _rail2LeftSigAmpList[j][k];
                    rail2LeftMax = Math.Max(_rail2LeftSigAmpList[j][k], rail2LeftMax);
                    setPointMarker(dPRail2LeftSigAmp, k);
                    dataSeries.DataPoints.Add(dPRail2LeftSigAmp);
                }
            }
            changeYAxisMax(chartRail2LeftSignalAmplitude, rail2LeftMax);
            chartRail2LeftSignalAmplitude.Series.Add(dataSeries);

            chartRail2RightSignalAmplitude.Series.Clear();
            dataSeries = new DataSeries();
            dataSeries.RenderAs = RenderAs.Line;
            dataSeries.XValueType = ChartValueTypes.DateTime;
            DataPoint dPRail2RightSigAmp;
            int rail2RightMax = 0;
            for (int j = 0; j < _rail2RightSigAmpList.Count; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    dPRail2RightSigAmp = new DataPoint();
                    dPRail2RightSigAmp.XValue = _dateTimeList[j].AddSeconds(k);
                    dPRail2RightSigAmp.YValue = _rail2RightSigAmpList[j][k];
                    rail2RightMax = Math.Max(_rail2RightSigAmpList[j][k], rail2RightMax);
                    setPointMarker(dPRail2RightSigAmp, k);
                    dataSeries.DataPoints.Add(dPRail2RightSigAmp);
                }
            }
            changeYAxisMax(chartRail2RightSignalAmplitude, rail2RightMax);
            chartRail2RightSignalAmplitude.Series.Add(dataSeries);
        }

        private void setPointMarker(DataPoint dp,int index)
        {
            dp.MarkerEnabled = true;
            if (index == 0)
            {
                dp.MarkerType = MarkerTypes.Triangle;
            }
            else
            {
                dp.MarkerType = MarkerTypes.Circle;
            }
        }

        private void changeYAxisMax(Chart chart2Change, int max)
        {
            if (max != 0)
            {
                Axis newYAxis = new Axis();
                newYAxis.AxisMaximum = (int)(max * 1.1);
                newYAxis.AxisMinimum = 0;
                newYAxis.Interval = (int)(max * 0.11);
                chart2Change.AxesY.Clear();
                chart2Change.AxesY.Add(newYAxis);
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
        private void chartTemprature_Rendered(object sender, EventArgs e)
        {
            var c = sender as Chart;
            var legend = c.Legends[0];
            var root = legend.Parent as Grid;
            //移除水印
            if (root != null)
            {
                root.Children.RemoveAt(11);
            }

            //lineChart.HideIndicator();
            //root.Children.RemoveAt(10);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _uniqueInstance = null;
        }

        private void btnFullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (_charts.Count == 0)
            {
                _charts.Add(chartTemprature);
                _charts.Add(chartRail1ThisAmplitude);
                _charts.Add(chartRail2ThisAmplitude);
                _charts.Add(chartRail1Stress);
                _charts.Add(chartRail2Stress);
                _charts.Add(chartRail1LeftSignalAmplitude);
                _charts.Add(chartRail1RightSignalAmplitude);
                _charts.Add(chartRail2LeftSignalAmplitude);
                _charts.Add(chartRail2RightSignalAmplitude);
            }
            bool hasCheckBoxChecked = false;
            for (int i = 0; i < 9; i++)
            {
                if (_checkBoxes[i].IsChecked.HasValue)
                {
                    if (_checkBoxes[i].IsChecked.Value)
                    {
                        hasCheckBoxChecked = true;
                    }
                }
            }
            if (!hasCheckBoxChecked)
            {
                MessageBox.Show("没有图表被选中！");
                return;
            }
            _originChartWidth = chartTemprature.ActualWidth;
            _originChartHeight = chartTemprature.ActualHeight;
            this.gridChart.Children.Clear();
            _fullScreenWin = new FullScreenChartWindow();
            _fullScreenWin.Closed += fullScreenWin_Closed;
            for (int i = 0; i < 9; i++)
            {
                if (_checkBoxes[i].IsChecked.HasValue)
                {
                    if (_checkBoxes[i].IsChecked.Value)
                    {
                        _fullScreenWin.gridFullScreen.Children.Add(_charts[i]);
                    }
                }
            }
            switch (_fullScreenWin.gridFullScreen.Children.Count)
            {
                case 1:
                    {
                        Chart aChart = _fullScreenWin.gridFullScreen.Children[0] as Chart;
                        if (aChart != null)
                        {
                            aChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
                            aChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                        }
                    }
                    break;
                case 2:
                    {
                        ColumnDefinition oneColumn = new ColumnDefinition();
                        ColumnDefinition twoColumn = new ColumnDefinition();
                        _fullScreenWin.gridFullScreen.ColumnDefinitions.Add(oneColumn);
                        _fullScreenWin.gridFullScreen.ColumnDefinitions.Add(twoColumn);

                        Chart oneChart = _fullScreenWin.gridFullScreen.Children[0] as Chart;
                        Chart twoChart = _fullScreenWin.gridFullScreen.Children[1] as Chart;
                        if (oneChart != null && twoChart != null)
                        {
                            oneChart.SetValue(Grid.RowProperty, 0);
                            oneChart.SetValue(Grid.ColumnProperty, 0);
                            twoChart.SetValue(Grid.RowProperty, 0);
                            twoChart.SetValue(Grid.ColumnProperty, 1);
                            oneChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth / 2;
                            oneChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                            twoChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth / 2;
                            twoChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                        }
                    }
                    break;
                case 3:
                    {
                        ColumnDefinition oneColumn = new ColumnDefinition();
                        ColumnDefinition twoColumn = new ColumnDefinition();
                        ColumnDefinition threeColumn = new ColumnDefinition();
                        _fullScreenWin.gridFullScreen.ColumnDefinitions.Add(oneColumn);
                        _fullScreenWin.gridFullScreen.ColumnDefinitions.Add(twoColumn);
                        _fullScreenWin.gridFullScreen.ColumnDefinitions.Add(threeColumn);

                        Chart oneChart = _fullScreenWin.gridFullScreen.Children[0] as Chart;
                        Chart twoChart = _fullScreenWin.gridFullScreen.Children[1] as Chart;
                        Chart threeChart = _fullScreenWin.gridFullScreen.Children[2] as Chart;
                        if (oneChart != null && twoChart != null && threeChart != null)
                        {
                            oneChart.SetValue(Grid.RowProperty, 0);
                            oneChart.SetValue(Grid.ColumnProperty, 0);
                            twoChart.SetValue(Grid.RowProperty, 0);
                            twoChart.SetValue(Grid.ColumnProperty, 1);
                            threeChart.SetValue(Grid.RowProperty, 0);
                            threeChart.SetValue(Grid.ColumnProperty, 2);
                            oneChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth / 3;
                            oneChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                            twoChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth / 3;
                            twoChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                            threeChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth / 3;
                            threeChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                        }
                    }
                    break;
                case 4:
                    {
                        ColumnDefinition oneColumn = new ColumnDefinition();
                        ColumnDefinition twoColumn = new ColumnDefinition();
                        RowDefinition oneRow = new RowDefinition();
                        RowDefinition twoRow = new RowDefinition();
                        _fullScreenWin.gridFullScreen.ColumnDefinitions.Add(oneColumn);
                        _fullScreenWin.gridFullScreen.ColumnDefinitions.Add(twoColumn);
                        _fullScreenWin.gridFullScreen.RowDefinitions.Add(oneRow);
                        _fullScreenWin.gridFullScreen.RowDefinitions.Add(twoRow);

                        Chart oneChart = _fullScreenWin.gridFullScreen.Children[0] as Chart;
                        Chart twoChart = _fullScreenWin.gridFullScreen.Children[1] as Chart;
                        Chart threeChart = _fullScreenWin.gridFullScreen.Children[2] as Chart;
                        Chart fourChart = _fullScreenWin.gridFullScreen.Children[3] as Chart;

                        if (oneChart != null && twoChart != null && threeChart != null && fourChart != null)
                        {
                            oneChart.SetValue(Grid.RowProperty, 0);
                            oneChart.SetValue(Grid.ColumnProperty, 0);
                            twoChart.SetValue(Grid.RowProperty, 0);
                            twoChart.SetValue(Grid.ColumnProperty, 1);
                            threeChart.SetValue(Grid.RowProperty, 1);
                            threeChart.SetValue(Grid.ColumnProperty, 0);
                            fourChart.SetValue(Grid.RowProperty, 1);
                            fourChart.SetValue(Grid.ColumnProperty, 1);
                            oneChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth / 2;
                            oneChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
                            twoChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth / 2;
                            twoChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
                            threeChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth / 2;
                            threeChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
                            fourChart.Width = System.Windows.SystemParameters.PrimaryScreenWidth / 2;
                            fourChart.Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
                        }
                    }
                    break;
                default:
                    break;
            }
            _fullScreenWin.Show();
        }

        private void fullScreenWin_Closed(object sender, EventArgs e)
        {
            for (int i = 0; i < 9; i++)
            {
                _fullScreenWin.gridFullScreen.Children.Clear();
                this.gridChart.Children.Add(_charts[i]);
                _charts[i].SetValue(Grid.RowProperty, i / 3);
                _charts[i].SetValue(Grid.ColumnProperty, i % 3);
                _charts[i].SetValue(Panel.ZIndexProperty, 10);
                _charts[i].Width = _originChartWidth;
                _charts[i].Height = _originChartHeight;
            }
            for (int i = 0; i < 9; i++)
            {
                this.gridChart.Children.Add(_checkBoxes[i]);
                _checkBoxes[i].SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Right);
                _checkBoxes[i].SetValue(VerticalAlignmentProperty, VerticalAlignment.Top);
                _checkBoxes[i].SetValue(Grid.RowProperty, i / 3);
                _checkBoxes[i].SetValue(Grid.ColumnProperty, i % 3);
                _checkBoxes[i].SetValue(Panel.ZIndexProperty, 100);
            }
        }
    }
}
