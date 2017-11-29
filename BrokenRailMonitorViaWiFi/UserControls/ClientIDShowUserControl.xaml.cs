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

namespace BrokenRailMonitorViaWiFi.UserControls
{
    /// <summary>
    /// Interaction logic for ClientIDShowUserControl.xaml
    /// </summary>
    public partial class ClientIDShowUserControl : UserControl
    {
        public static readonly DependencyProperty ClientIDProperty = DependencyProperty.Register("ClientID", typeof(int), typeof(ClientIDShowUserControl));

        public int ClientID
        {
            get { return (int)GetValue(ClientIDProperty); }
            set { SetValue(ClientIDProperty, value); }
        }

        public ClientIDShowUserControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }
}
