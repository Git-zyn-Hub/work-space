// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   No color coding, use coloured lights
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Collections.Generic;

namespace SurfacePlot
{
    // http://reference.wolfram.com/mathematica/tutorial/ThreeDimensionalSurfacePlots.html

    public enum ColorCoding
    {
        /// <summary>
        /// No color coding, use coloured lights
        /// </summary>
        ByLights,

        /// <summary>
        /// Color code by gradient in y-direction using a gradient brush with white ambient light
        /// </summary>
        ByGradientY
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public double Zmax { get; set; }
        public double ZStep { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }

        public Func<double, double, double> Function { get; set; }
        public Point3D[,] Data { get; set; }
        public double[,] ColorValues { get; set; }
        public Brush ColorScheme { get; set; }
        private List<int[]> _amplitudeOf4Frequency;

        public ColorCoding ColorCoding { get; set; }

        public Model3DGroup Lights
        {
            get
            {
                var group = new Model3DGroup();
                switch (ColorCoding)
                {
                    case ColorCoding.ByGradientY:
                        group.Children.Add(new AmbientLight(Colors.White));
                        break;
                    case ColorCoding.ByLights:
                        group.Children.Add(new AmbientLight(Colors.Gray));
                        group.Children.Add(new PointLight(Colors.Red, new Point3D(0, -1000, 0)));
                        group.Children.Add(new PointLight(Colors.Blue, new Point3D(0, 0, 1000)));
                        group.Children.Add(new PointLight(Colors.Green, new Point3D(1000, 1000, 0)));
                        break;
                }
                return group;
            }
        }

        public Brush SurfaceBrush
        {
            get
            {
                // Brush = BrushHelper.CreateGradientBrush(Colors.White, Colors.Blue);
                // Brush = GradientBrushes.RainbowStripes;
                // Brush = GradientBrushes.BlueWhiteRed;
                switch (ColorCoding)
                {
                    case ColorCoding.ByGradientY:
                        //return BrushHelper.CreateGradientBrush(Colors.Blue, Colors.White, Colors.Red);
                        return MyBrushHelper.CreateRainbowBrush(false);
                    case ColorCoding.ByLights:
                        return Brushes.White;
                }
                return null;
            }
        }

        public MainViewModel(List<int[]> amplitudeOf4Frequency)
        {
            //MinX = 0;
            //MaxX = 30;
            //MinY = 0;
            //MaxY = 30;
            //Rows = 31;
            //Columns = 31;

            _amplitudeOf4Frequency = amplitudeOf4Frequency;
            //Function = (x, y) => Math.Sin(x * y) * 0.5;
            ColorCoding = ColorCoding.ByGradientY;
            ColorScheme = BrushHelper.CreateRainbowBrush(false);
            //ColorScheme = BrushHelper.CreateGradientBrush(new List<Color> { Colors.Red, Colors.White, Colors.Blue }, false);
            UpdateModel();
        }

        public float[,,] controlPoint = new float[4, 4, 3]
       {
            {
                { 0,0,24499000}, {0,10,2}, {0,20,5}, {0,30,10000000}
            },
            {
                { 10,0,2}, {10,10,1000 }, {10, 20,10000000}, {10,30,3}
            },
            {
                { 20,0,5}, {20,10,2 }, {20,20,30000000 }, {20,30,0 }
            },
            {
                {30,0,0 }, {30,10,3 }, {30,20,3 }, { 30,30,5}
            }
       };

        private void UpdateModel()
        {
            //Data = CreateDataArray(Function);
            int columnReal = 0;
            if (_amplitudeOf4Frequency == null || _amplitudeOf4Frequency.Count == 0)
            {
                return;
            }
            columnReal = _amplitudeOf4Frequency.Count;
            Point3D[,] pointThrough = new Point3D[columnReal, 4];
            int column = (columnReal - 1) * 10 + 1;
            Data = new Point3D[column, 31];

            Zmax = 0;
            //Data = new Point3D[4, 4];
            for (int i = 0; i < columnReal; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Point3D newp = new Point3D(i * 10, j * 10, Transform(_amplitudeOf4Frequency[i][j]));
                    Zmax = Math.Max(Zmax, _amplitudeOf4Frequency[i][j]);
                    pointThrough[i, j] = newp;
                }
            }
            Zmax = ((int)Zmax) / 10 * 10;
            ZStep = Zmax / 10;

            for (int i = 0; i < columnReal; i++)
            {
                PointEdgeNear0Calc c = new PointEdgeNear0Calc();
                c.PointThroughNear0 = pointThrough[i, 0];
                c.PointThroughMiddle = pointThrough[i, 1];
                c.PointThroughFar0 = pointThrough[i, 2];

                List<Point3D> resultEdgeNear0 = c.Calc(false);

                PointMiddleCalc pc = new PointMiddleCalc();
                pc.Point0ThroughNear0 = pointThrough[i, 0];
                pc.Point1ThroughMiddleNear0 = pointThrough[i, 1];
                pc.Point2ThroughMiddleFar0 = pointThrough[i, 2];
                pc.Point3ThroughFar0 = pointThrough[i, 3];

                List<Point3D> resultMiddle = pc.Calc(false);

                PointEdgeFar0Calc pe = new PointEdgeFar0Calc();
                pe.PointThroughNear0 = pointThrough[i, 1];
                pe.PointThroughMiddle = pointThrough[i, 2];
                pe.PointThroughFar0 = pointThrough[i, 3];

                List<Point3D> resultEdgeFar0 = pe.Calc(false);

                for (int j = 0; j < 10; j++)
                {
                    Data[i * 10, j] = resultEdgeNear0[j];
                    Data[i * 10, j + 10] = resultMiddle[j];
                    Data[i * 10, j + 20] = resultEdgeFar0[j];
                }
                Data[i * 10, 30] = pointThrough[i, 3];
            }

            if (columnReal > 1)
            {
                for (int i = 0; i < 31; i++)
                {
                    PointEdgeNear0Calc c = new PointEdgeNear0Calc();
                    c.PointThroughNear0 = Data[0, i];
                    c.PointThroughMiddle = Data[10, i];
                    c.PointThroughFar0 = columnReal > 2 ? Data[20, i] : new Point3D(20, i, Data[10, i].Z);
                    List<Point3D> resultEdgeNear0 = c.Calc(true);

                    for (int j = 1; j < 10; j++)
                    {
                        Data[j, i] = resultEdgeNear0[j];
                    }

                    if (columnReal > 2)
                    {
                        for (int k = 0; k <= columnReal * 10 - 40; k += 10)
                        {
                            PointMiddleCalc pc = new PointMiddleCalc();
                            pc.Point0ThroughNear0 = Data[k, i];
                            pc.Point1ThroughMiddleNear0 = Data[k + 10, i];
                            pc.Point2ThroughMiddleFar0 = Data[k + 20, i];
                            pc.Point3ThroughFar0 = Data[k + 30, i];
                            List<Point3D> resultMiddle = pc.Calc(true);

                            for (int j = 1; j < 10; j++)
                            {
                                Data[k + j + 10, i] = resultMiddle[j];
                            }
                        }

                        PointEdgeFar0Calc pe = new PointEdgeFar0Calc();
                        pe.PointThroughNear0 = Data[columnReal * 10 - 30, i];
                        pe.PointThroughMiddle = Data[columnReal * 10 - 20, i];
                        pe.PointThroughFar0 = Data[columnReal * 10 - 10, i];
                        List<Point3D> resultEdgeFar0 = pe.Calc(true);

                        for (int j = 1; j < 10; j++)
                        {
                            Data[j + columnReal * 10 - 20, i] = resultEdgeFar0[j];
                        }
                    }
                }
            }
            switch (ColorCoding)
            {
                case ColorCoding.ByGradientY:
                    ColorValues = FindGradientY(Data);
                    break;
                case ColorCoding.ByLights:
                    ColorValues = null;
                    break;
            }
            RaisePropertyChanged("Data");
            RaisePropertyChanged("ColorValues");
            RaisePropertyChanged("SurfaceBrush");
            RaisePropertyChanged("Zmax");
            RaisePropertyChanged("ZStep");
        }

        public Point GetPointFromIndex(int i, int j)
        {
            double x = MinX + (double)j / (Columns - 1) * (MaxX - MinX);
            double y = MinY + (double)i / (Rows - 1) * (MaxY - MinY);
            return new Point(x, y);
        }

        public Point3D[,] CreateDataArray(Func<double, double, double> f)
        {
            var data = new Point3D[Rows, Columns];
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                {
                    var pt = GetPointFromIndex(i, j);
                    data[i, j] = new Point3D(pt.X, pt.Y, f(pt.X, pt.Y));
                }
            return data;
        }

        // http://en.wikipedia.org/wiki/Numerical_differentiation
        public double[,] FindGradientY(Point3D[,] data)
        {
            int n = data.GetUpperBound(0) + 1;
            int m = data.GetUpperBound(1) + 1;
            var K = new double[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    // Finite difference approximation
                    var p10 = data[i + 1 < n ? i + 1 : i, j - 1 > 0 ? j - 1 : j];
                    var p00 = data[i - 1 > 0 ? i - 1 : i, j - 1 > 0 ? j - 1 : j];
                    var p11 = data[i + 1 < n ? i + 1 : i, j + 1 < m ? j + 1 : j];
                    var p01 = data[i - 1 > 0 ? i - 1 : i, j + 1 < m ? j + 1 : j];

                    //double dx = p01.X - p00.X;
                    //double dz = p01.Z - p00.Z;
                    //double Fx = dz / dx;

                    double dy = p10.Y - p00.Y;
                    double dz = p10.Z - p00.Z;

                    K[i, j] = dz / dy;
                }
            return K;
        }

        public double Transform(double x)
        {
            double y = 0;
            //y = x > 1 ? 2 * Math.Log10(x) + 1 : x;
            y = x / 4000000;
            return y;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

    }
}