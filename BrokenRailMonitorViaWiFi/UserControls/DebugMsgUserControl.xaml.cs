using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// DebugMsgUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class DebugMsgUserControl : UserControl
    {
        private bool _stopScroll = false;

        public DebugMsgUserControl()
        {
            InitializeComponent();
        }

        public void AppendMsg(string msg)
        {

            int startLength = txtBox.Text.Length;
            txtBox.Text += msg;
            ScrollControl(startLength, msg.Length);
        }

        public void ClearMsg()
        {
            txtBox.Text = string.Empty;
        }


        private void ScrollControl(int startLength, int byteCount)
        {
            if (!_stopScroll)
            {
                //自动滚动到底部
                txtBox.Focus();
                txtBox.Select(startLength, byteCount);//光标定位到文本最后
            }
        }
        private void tbtnPin_Checked(object sender, RoutedEventArgs e)
        {
            _stopScroll = true;
        }

        private void tbtnPin_Unchecked(object sender, RoutedEventArgs e)
        {
            _stopScroll = false;
        }

        private void ToggleButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ToggleButton b = sender as ToggleButton;
            if (b != null)
            {
                b.BorderThickness = new Thickness(0);
            }
        }

        private void ToggleButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ToggleButton b = sender as ToggleButton;
            if (b != null)
            {
                b.BorderThickness = new Thickness(1);
            }
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            Button b = sender as Button;
            if (b != null)
            {
                b.BorderThickness = new Thickness(0);
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Button b = sender as Button;
            if (b != null)
            {
                b.BorderThickness = new Thickness(1);
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearMsg();
        }
    }
}
