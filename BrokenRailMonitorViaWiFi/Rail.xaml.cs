using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace BrokenRail3MonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for Rail.xaml
    /// </summary>
    public partial class Rail : UserControl,INotifyPropertyChanged
    {
        private readonly int _railNumber;
        private RailStates _railState = RailStates.IsError;
        //private RailNo _whichRail;
        public int RailNumber
        {
            get
            {
                return _railNumber;
            }
        }

        public RailStates RailState
        {
            get
            {
                return _railState;
            }

            set
            {
                _railState = value;
            }
        }

        //public RailNo WhichRail
        //{
        //    get
        //    {
        //        return _whichRail;
        //    }

        //    set
        //    {
        //        if (_whichRail != value)
        //        {
        //            _whichRail = value;
        //            OnPropertyChanged("WhichRail");
        //        }
        //    }
        //}


        public Rail()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        public Rail(int railNumber)
        {
            InitializeComponent();
            this._railNumber = railNumber;
            this.DataContext = this;
        }
        public void Error()
        {
            this.RailState = RailStates.IsError;
            this.recRail.Fill = new SolidColorBrush(Colors.Red);
        }

        public void Normal()
        {
            this.RailState = RailStates.IsNormal;
            this.recRail.Fill = new SolidColorBrush(Colors.LightGreen);
        }

        public void Different()
        {
            this.RailState = RailStates.IsDifferent;
            this.recRail.Fill = new SolidColorBrush(Colors.Orange);
        }

        public void Timeout()
        {
            this.RailState = RailStates.IsTimeout;
            this.recRail.Fill = new SolidColorBrush(Colors.Gray);
        }

        public void ContinuousInterference()
        {
            this.RailState = RailStates.IsContinuousInterference;
            this.recRail.Fill = new SolidColorBrush(Colors.LightBlue);
        }
        private void railUserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
        
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }

    public enum RailStates
    {
        IsError,
        IsNormal,
        IsDifferent,
        IsTimeout,
        IsContinuousInterference
    }
}
