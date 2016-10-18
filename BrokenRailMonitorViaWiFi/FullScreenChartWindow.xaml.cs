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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrokenRailMonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for FullScreenChartWindow.xaml
    /// </summary>
    public partial class FullScreenChartWindow : Window
    {
        private DispatcherTimer _timerCloseRecentPopup;
        public FullScreenChartWindow()
        {
            InitializeComponent();
            if (_timerCloseRecentPopup == null)
            {
                _timerCloseRecentPopup = new DispatcherTimer();
                _timerCloseRecentPopup.Interval = new TimeSpan(0, 0, 3);
                _timerCloseRecentPopup.Tick += closePopup;
            }
        }
        private void CommandBinding_cmdExitFullScreen_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.gridFullScreen.IsVisible;
        }

        private void CommandBinding_cmdExitFullScreen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void fullScreenWin_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
            this.Topmost = true;

            this.ppuToolTip.IsOpen = true;
            this._timerCloseRecentPopup.Start();
            this.ppuToolTip.PopupAnimation = PopupAnimation.Fade;
            this.ppuToolTip.StaysOpen = false;
        }
        private void closePopup(object sender, EventArgs e)
        {
            this.ppuToolTip.IsOpen = false;
            this._timerCloseRecentPopup.Stop();
        }
    }
}
