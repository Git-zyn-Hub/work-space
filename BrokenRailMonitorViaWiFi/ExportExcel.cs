using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using excel = ClosedXML.Excel;
using io = System.IO;

namespace BrokenRail3MonitorViaWiFi
{
    public class ExportExcel
    {
        private List<MasterControl> _masterControlList;
        private List<DateTime> _dateTimeList = new List<DateTime>();
        private int[] _rail1Temprature;
        private int[] _rail2Temprature;
        private int[] _terminalTemprature;
        private List<int[]> _rail1LeftSigAmpList;
        private List<int[]> _rail1RightSigAmpList;
        private string _fileName;
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

        public List<MasterControl> MasterControlList
        {
            get
            {
                return _masterControlList;
            }

            set
            {
                _masterControlList = value;
            }
        }
        public ExportExcel(List<MasterControl> masterControlList)
        {
            try
            {
                this.MasterControlList = masterControlList;
                DateTime now = System.DateTime.Now;
                string directoryName = now.ToString("yyyy") + "\\" + now.ToString("yyyy-MM");
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = System.Environment.CurrentDirectory + @"\DataRecord\" + directoryName;
                openFileDialog.Filter = "xml files(*.xml)|*.xml";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.FilterIndex = 1;
                if (openFileDialog.ShowDialog().Value == true)
                {
                    try
                    {
                        string fName = openFileDialog.FileName;
                        int index = fName.LastIndexOf("\\");
                        string fileNameJudge = fName.Substring(index + 1, 12);
                        string strTerminalNo = fName.Substring(index + 13, 3);
                        int terminalNo = 0;
                        if (fileNameJudge == "DataTerminal")
                        {
                            if (int.TryParse(strTerminalNo, out terminalNo))
                            {
                                int indexOfMaster = FindMasterControlIndex(terminalNo);
                                if (indexOfMaster == -1)
                                {
                                    MessageBox.Show("读取的终端号在终端集合中不存在！");
                                    return;
                                }
                                else
                                {
                                    this.FileName = fName;
                                    this.SaveResult();
                                }
                            }
                            else
                            {
                                MessageBox.Show("终端号无法转换！更改文件名时请保留前15位！");
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show("文件名发生改变，无法获得历史数据的终端号！\r\n更改文件名时请保留前15位");
                            return;
                        }
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show("文件打开异常！" + ee.Message);
                    }
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show("读取文件异常！" + ee.Message);
            }
        }

        /// <summary>
        /// 根据终端号寻找终端所在List的索引。
        /// </summary>
        /// <param name="terminalNo">终端号</param>
        /// <returns>如果找到返回索引，否则返回-1</returns>
        public int FindMasterControlIndex(int terminalNo)
        {
            int i = 0;
            foreach (var item in this.MasterControlList)
            {
                if (item.TerminalNumber == terminalNo)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }


        public void SaveResult()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(FileName);
                XmlNodeList xn0 = xmlDoc.SelectSingleNode("Datas").ChildNodes;
                handleDataAndSaveExcel(xn0);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void handleDataAndSaveExcel(XmlNodeList xnl)
        {
            try
            {
                int nodeCount = xnl.Count;
                _dateTimeList.Clear();
                _rail1Temprature = new int[nodeCount];
                _rail2Temprature = new int[nodeCount];
                _terminalTemprature = new int[nodeCount];
                _rail1LeftSigAmpList = new List<int[]>();
                _rail1RightSigAmpList = new List<int[]>();
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
                    i++;
                }
                DataTable dt = new DataTable();
                dt.Columns.Add("时间", typeof(DateTime));
                dt.Columns.Add("轨1温度", typeof(int));
                dt.Columns.Add("轨2温度", typeof(int));
                dt.Columns.Add("终端温度", typeof(int));
                dt.Columns.Add("轨1左信号幅值1", typeof(int));
                dt.Columns.Add("轨1左信号幅值2", typeof(int));
                dt.Columns.Add("轨1左信号幅值3", typeof(int));
                dt.Columns.Add("轨1左信号幅值4", typeof(int));
                dt.Columns.Add("轨1右信号幅值1", typeof(int));
                dt.Columns.Add("轨1右信号幅值2", typeof(int));
                dt.Columns.Add("轨1右信号幅值3", typeof(int));
                dt.Columns.Add("轨1右信号幅值4", typeof(int));

                for (int n = 0; n < nodeCount; n++)
                {
                    DataRow row = dt.NewRow();
                    row["时间"] = _dateTimeList[n];
                    row["轨1温度"] = _rail1Temprature[n];
                    row["轨2温度"] = _rail2Temprature[n];
                    row["终端温度"] = _terminalTemprature[n];
                    row["轨1左信号幅值1"] = _rail1LeftSigAmpList[n][0];
                    row["轨1左信号幅值2"] = _rail1LeftSigAmpList[n][1];
                    row["轨1左信号幅值3"] = _rail1LeftSigAmpList[n][2];
                    row["轨1左信号幅值4"] = _rail1LeftSigAmpList[n][3];
                    row["轨1右信号幅值1"] = _rail1RightSigAmpList[n][0];
                    row["轨1右信号幅值2"] = _rail1RightSigAmpList[n][1];
                    row["轨1右信号幅值3"] = _rail1RightSigAmpList[n][2];
                    row["轨1右信号幅值4"] = _rail1RightSigAmpList[n][3];
                    dt.Rows.Add(row);
                }


                excel.XLWorkbook workBook = new excel.XLWorkbook();
                excel.IXLWorksheet workSheet = workBook.AddWorksheet("Sheet1");

                workSheet.Column(1).Style.NumberFormat.Format = "yyyy/m/d HH:mm:ss";
                //设置第1列的宽度
                workSheet.Column(1).Width = 18;
                //设置第2到4列的宽度
                workSheet.Columns(2, 4).Width = 8;
                //设置第5到12列的宽度
                workSheet.Columns(5, 12).Width = 16;
                for (int n = 0; n < dt.Columns.Count; n++)
                {
                    workSheet.Cell(1, n + 1).Value = dt.Columns[n].ColumnName;
                }
                for (int n = 0; n < dt.Rows.Count; n++)
                {
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        workSheet.Cell(n + 2, j + 1).Value = dt.Rows[n][j].ToString();
                    }
                }


                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Excel File|*.xlsx|All File|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                string fName = string.Empty;
                if (saveFileDialog.ShowDialog().Value)
                {
                    fName = saveFileDialog.FileName;
                    if (!io.File.Exists(fName))
                    {
                        io.File.Create(fName).Close();
                    }
                }
                if (!string.IsNullOrEmpty(fName))
                {
                    //给Excel文件添加"Everyone,Users"用户组的完全控制权限  
                    io.FileInfo fi = new io.FileInfo(fName);
                    FileSecurity fileSecurity = fi.GetAccessControl();
                    fileSecurity.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow));
                    fileSecurity.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fileSecurity);

                    //给Excel文件所在目录添加"Everyone,Users"用户组的完全控制权限  
                    io.DirectoryInfo di = new io.DirectoryInfo(io.Path.GetDirectoryName(fName));
                    DirectorySecurity dirSecurity = di.GetAccessControl();
                    dirSecurity.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow));
                    dirSecurity.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, AccessControlType.Allow));
                    di.SetAccessControl(dirSecurity);

                    try
                    {
                        workBook.SaveAs(fName);
                        MessageBox.Show("保存成功");
                    }
                    catch (io.IOException)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }
    }
}
