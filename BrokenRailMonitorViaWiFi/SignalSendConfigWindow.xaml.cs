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

namespace BrokenRail3MonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for SignalSendConfigWindow.xaml
    /// </summary>
    public partial class SignalSendConfigWindow : Window
    {
        private int _sendInterval;
        private int _sendTimeOpportunity;
        private int _neighbourSmallOpportunity;
        private int _neighbourBigOpportunity;

        public int SendInterval
        {
            get
            {
                return _sendInterval;
            }

            set
            {
                _sendInterval = value;
            }
        }

        public int SendTimeOpportunity
        {
            get
            {
                return _sendTimeOpportunity;
            }

            set
            {
                _sendTimeOpportunity = value;
            }
        }

        public int NeighbourSmallOpportunity
        {
            get
            {
                return _neighbourSmallOpportunity;
            }

            set
            {
                _neighbourSmallOpportunity = value;
            }
        }

        public int NeighbourBigOpportunity
        {
            get
            {
                return _neighbourBigOpportunity;
            }

            set
            {
                _neighbourBigOpportunity = value;
            }
        }

        public SignalSendConfigWindow()
        {
            InitializeComponent();
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(this.txtSendInterval.Text, out _sendInterval))
            {
                if (_sendInterval < 0 || _sendInterval > 255)
                {
                    MessageBox.Show("'发射间隔'必须在0到255之间");
                    this.txtSendInterval.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'发射间隔'必须输入0~255的整数");
                this.txtSendInterval.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtSendTimeOpportunity.Text, out _sendTimeOpportunity))
            {
                if (_sendTimeOpportunity < 0 || _sendTimeOpportunity > 255)
                {
                    MessageBox.Show("'发射时机'必须在0到255之间");
                    this.txtSendTimeOpportunity.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'发射时机'必须输入0~255的整数");
                this.txtSendTimeOpportunity.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtNeighbourSmallOpportunity.Text, out _neighbourSmallOpportunity))
            {
                if (_neighbourSmallOpportunity < 0 || _neighbourSmallOpportunity > 255)
                {
                    MessageBox.Show("'相邻小终端发射时机'必须在0到255之间");
                    this.txtNeighbourSmallOpportunity.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'相邻小终端发射时机'必须输入0~255的整数");
                this.txtNeighbourSmallOpportunity.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtNeighbourBigOpportunity.Text, out _neighbourBigOpportunity))
            {
                if (_neighbourBigOpportunity < 0 || _neighbourBigOpportunity > 255)
                {
                    MessageBox.Show("'相邻大终端发射时机'必须在0到255之间");
                    this.txtNeighbourBigOpportunity.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'相邻大终端发射时机'必须输入0~255的整数");
                this.txtNeighbourBigOpportunity.Text = string.Empty;
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            /*e.Key != Key.Back && e.Key != Key.Decimal && e.Key != Key.OemPeriod && e.Key != Key.Return
            * Key.Back 退格键
            * Key.Return 回车
            * Key.Tab 键
            */
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Back || e.Key == Key.Return || e.Key == Key.Tab)
            {
                e.Handled = false;
            }
            else if ((e.Key >= Key.D0 && e.Key <= Key.D9) && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //屏蔽中文输入和非法字符粘贴输入
            try
            {
                TextBox textBox = sender as TextBox;
                TextChange[] change = new TextChange[e.Changes.Count];
                e.Changes.CopyTo(change, 0);

                int offset = change[0].Offset;
                if (change[0].AddedLength > 0)
                {
                    //这里只做Double类型转换的检测，如果是Int或者其他类型需要改变num的类型，和TryParse前面类型。
                    int num = 0;
                    if (!int.TryParse(textBox.Text, out num))
                    {
                        textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                        textBox.Select(offset, 0);
                    }
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
                throw;
            }
        }
    }
}
