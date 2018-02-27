using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SurfacePlot
{
    public class EndPoint3D : ParametricSurface3D
    {
        private double[] _endPoint;
        private float _r = 0.3f;
        private HelixViewport3D _viewport;
        private ToolTip _tip = new ToolTip();
        private double _originZValue;
        public double[] EndPoint
        {
            get { return _endPoint; }
            set
            {
                _endPoint = value;
            }
        }

        public float R
        {
            get
            {
                return _r;
            }

            set
            {
                _r = value;
            }
        }

        public EndPoint3D(double[] endpoint, HelixViewport3D viewport,double originZValue)
        {
            EndPoint = endpoint;
            _viewport = viewport;
            _originZValue = originZValue;
            _tip.Content = _originZValue.ToString();
            _tip.Placement = PlacementMode.Mouse;
            _viewport.QueryCursor += Viewport_QueryCursor;
            this.Fill = new SolidColorBrush(Colors.Red);
            UpdateModel();
        }

        private void Viewport_QueryCursor(object sender, System.Windows.Input.QueryCursorEventArgs e)
        {
            if (_viewport.CursorOnElementPosition.HasValue)
            {
                if ((_viewport.CursorOnElementPosition.Value.X < EndPoint[0] - R || _viewport.CursorOnElementPosition.Value.X > EndPoint[0] + R)
                    || (_viewport.CursorOnElementPosition.Value.Y < EndPoint[1] - R || _viewport.CursorOnElementPosition.Value.Y > EndPoint[1] + R)
                    || (_viewport.CursorOnElementPosition.Value.Z < EndPoint[2] - R || _viewport.CursorOnElementPosition.Value.Z > EndPoint[2] + R))
                {
                    _tip.IsOpen = false;
                    this.Fill = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    _tip.IsOpen = true;
                    this.Fill = new SolidColorBrush(Colors.Gold);
                }
            }
            else
            {
                _tip.IsOpen = false;
                this.Fill = new SolidColorBrush(Colors.Red);
            }
        }

        const double pi = Math.PI;
        public double cos(double x) { return Math.Cos(x); }
        public double sin(double x) { return Math.Sin(x); }
        public double abs(double x) { return Math.Abs(x); }
        public double sqrt(double x) { return Math.Sqrt(x); }
        public double sign(double x) { return Math.Sign(x); }
        public double sqr(double x) { return x * x; }
        public double log(double x) { return Math.Log(x); }
        public double exp(double x) { return Math.Exp(x); }
        public double pow(double x, double y) { return Math.Pow(x, y); }
        protected override Point3D Evaluate(double u, double v, out Point textureCoord)
        {
            if (EndPoint == null)
            {
                textureCoord = new Point();
                return new Point3D(0, 0, 0);
            }

            u *= pi;
            v *= 2 * pi;
            var p = new Point3D();
            textureCoord = new Point(u, v);
            p.X = EndPoint[0] + R * sin(u) * cos(v);
            p.Y = EndPoint[1] + R * sin(u) * sin(v);
            p.Z = EndPoint[2] + R * cos(u);

            return new Point3D(p.X, p.Y, p.Z);
        }
    }
}
