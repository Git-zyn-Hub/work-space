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

namespace SurfacePlot
{
    /// <summary>
    /// Interaction logic for SurfacePlot.xaml
    /// </summary>
    public partial class SurfacePlot3D : UserControl, INotifyPropertyChanged
    {
        private List<int[]> _amplitudeOf4Frequency;
        private MainViewModel _mViewModel;
        private string _title;

        public string Title
        {
            get
            {
                return _title;
            }

            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged("Title");
                }
            }
        }

        public List<int[]> AmplitudeOf4Frequency
        {
            get
            {
                return _amplitudeOf4Frequency;
            }

            set
            {
                _amplitudeOf4Frequency = value;
                _mViewModel = new MainViewModel(_amplitudeOf4Frequency);
                this.DataContext = _mViewModel;
            }
        }

        public SurfacePlot3D()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_amplitudeOf4Frequency == null || _amplitudeOf4Frequency.Count == 0)
            {
                return;
            }
            for (int i = 0; i < _amplitudeOf4Frequency.Count; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    double[] EndPoint = new double[3]
                    {
                        i*10,
                        j*10,
                        _mViewModel.Transform(_amplitudeOf4Frequency[i][j])
                    };
                    EndPoint3D endPoint = new EndPoint3D(EndPoint, hvp3D, _amplitudeOf4Frequency[i][j]);
                    endPoint.MeshSizeU = 10;
                    endPoint.MeshSizeV = 10;
                    hvp3D.Children.Add(endPoint);
                }
            }
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
}
