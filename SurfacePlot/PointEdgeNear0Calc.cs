using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Windows;

namespace SurfacePlot
{
    public class PointEdgeNear0Calc : PointBase
    {
        public Point3D PointThroughNear0 { get; set; }
        public Point3D PointThroughMiddle { get; set; }
        public Point3D PointThroughFar0 { get; set; }
        /*求根公式，求固定X0值对应的d值。
         * 设Point3D P10=Mul(a,b,d);
         *   Point3D P11=Mul(c,e,d);
         * 则Point3D result=Mul(P11，P10，d);
         * 代入是之后是一个关于d的一元二次方程，已知一元二次方程的求根公式为[-B+（or）-sqrt(pow(B,2)-4*A*C)]/(2*A)
         * 对应的系数A=c.X-e.X-a.X+b.X
         *          B=a.X-2*b.X+e.X
         *          C=b.X-X0
         * 又注意到a==e;所以可以简化系数为 A=c.X+b.X-2*a.X;
         *                               B=2*(a.X-b.X);
         *                               C=b.X-X0
         */
        /// <summary>
        /// 计算4个经过点，靠近0的两个点，之间的插值点
        /// </summary>
        /// <param name="isHorizontal">true表示y值相同，计算x值；false表示x值相同，计算y值</param>
        /// <returns></returns>
        public override List<Point3D> Calc(bool isHorizontal)
        {
            List<Point3D> result = new List<Point3D>();

            Point3D center01 = Average(PointThroughNear0, PointThroughMiddle);
            Point3D center12 = Average(PointThroughMiddle, PointThroughFar0);

            double distance01 = Distance(PointThroughNear0, PointThroughMiddle);
            double distance12 = Distance(PointThroughMiddle, PointThroughFar0);

            double d = distance01 / (distance01 + distance12);
            Point3D Bi = Mul(center01, center12, d);

            Vector3D Bi2Middle = Sub(PointThroughMiddle, Bi).ToVector3D();

            Point3D controlPoint = Add(center01, Bi2Middle.ToPoint3D());

            for (int i = 0; i < 10; i++)
            {
                Point3D a = controlPoint;
                Point3D b = PointThroughNear0;
                Point3D c = PointThroughMiddle;
                double r;
                double A;
                double B;
                double C;
                if (isHorizontal)
                {
                    r = Mul(PointThroughMiddle, PointThroughNear0, (double)i / 10).X;
                    A = c.X + b.X - 2 * a.X;
                    B = 2 * (a.X - b.X);
                    C = b.X - r;
                }
                else
                {
                    r = Mul(PointThroughMiddle, PointThroughNear0, (double)i / 10).Y;
                    A = c.Y + b.Y - 2 * a.Y;
                    B = 2 * (a.Y - b.Y);
                    C = b.Y - r;
                }

                double xd = GetXFrom1VariableQuadraticEquation(A, B, C);
                Point3D P10 = Mul(controlPoint, PointThroughNear0, xd);
                Point3D P11 = Mul(PointThroughMiddle, controlPoint, xd);

                Point3D oneResult = Mul(P11, P10, xd);

                result.Add(oneResult);
            }
            return result;
        }
    }
}
