using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SurfacePlot
{
    public abstract class PointBase
    {
        /// <summary>
        /// 求点a,b连成线段的中点
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public Point3D Average(Point3D a, Point3D b)
        {
            return new Point3D((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public Point3D Add(Point3D a, Point3D b)
        {
            return new Point3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        public Point3D Sub(Point3D a, Point3D b)
        {
            return new Point3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
        /// <summary>
        /// 求线段ab的按照比例d得到的一点r，使rb:ab=d
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public Point3D Mul(Point3D a, Point3D b, double d)
        {
            Point3D temp = Sub(a, b);
            temp = new Point3D(temp.X * d, temp.Y * d, temp.Z * d);
            temp = Add(b, temp);
            return temp;
        }
        /// <summary>
        /// 求线段ab的长度
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public double Distance(Point3D a, Point3D b)
        {
            return sqrt(pow(a.X - b.X, 2) + pow(a.Y - b.Y, 2) + pow(a.Z - b.Z, 2));
        }

        public const double pi = Math.PI;
        public double cos(double x) { return Math.Cos(x); }
        public double sin(double x) { return Math.Sin(x); }
        public double abs(double x) { return Math.Abs(x); }
        public double sqrt(double x) { return Math.Sqrt(x); }
        public double sign(double x) { return Math.Sign(x); }
        public double sqr(double x) { return x * x; }
        public double log(double x) { return Math.Log(x); }
        public double exp(double x) { return Math.Exp(x); }
        public double pow(double x, double y) { return Math.Pow(x, y); }
        public double arccos(double x) { return Math.Acos(x); }

        public abstract List<Point3D> Calc(bool isHorizontal);

        /// <summary>
        /// 求一元二次方程的在[0,1]范围内的解
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        public double GetXFrom1VariableQuadraticEquation(double A, double B, double C)
        {
            double x = 0;
            if (A == 0)
            {
                if (B != 0)
                {
                    x = -C / B;
                }
                else
                {
                    MessageBox.Show("方程组找不到解！请查看输入的点。B=0");
                }
            }
            else
            {
                double xd0 = ((0 - B) + sqrt(pow(B, 2) - 4 * A * C)) / (2 * A);
                double xd1 = ((0 - B) - sqrt(pow(B, 2) - 4 * A * C)) / (2 * A);

                if ((xd0 >= 0 && xd0 <= 1) || (xd1 >= 0 && xd1 <= 1))
                {
                    x = (xd0 >= 0 && xd0 <= 1) ? xd0 : xd1;
                }
                else
                {
                    MessageBox.Show("方程组找不到解！请查看输入的点。解不在范围内");
                }
            }

            return x;
        }
    }
}
