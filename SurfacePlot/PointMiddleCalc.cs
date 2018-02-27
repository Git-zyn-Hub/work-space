using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Windows;
using System.Diagnostics;

namespace SurfacePlot
{
    public class PointMiddleCalc : PointBase
    {
        public Point3D Point0ThroughNear0 { get; set; }
        public Point3D Point1ThroughMiddleNear0 { get; set; }
        public Point3D Point2ThroughMiddleFar0 { get; set; }
        public Point3D Point3ThroughFar0 { get; set; }

        /*将三次Bezier公式展开得到如下公式 0=（-x0+3*x1-3*x2+x3)*pow(t,3)+3*(x0-2*x1+x2)*pow(t,2)+3*(-x0+x1)*t+x0-r.X; 
         */
        /// <summary>
        /// 计算4个经过点中间两点内部的插入点
        /// </summary>
        /// <param name="isHorizontal">true表示y值相同，计算x值；false表示x值相同，计算y值</param>
        /// <returns></returns>
        public override List<Point3D> Calc(bool isHorizontal)
        {
            List<Point3D> result = new List<Point3D>();

            Point3D center01 = Average(Point0ThroughNear0, Point1ThroughMiddleNear0);
            Point3D center12 = Average(Point1ThroughMiddleNear0, Point2ThroughMiddleFar0);
            Point3D center23 = Average(Point2ThroughMiddleFar0, Point3ThroughFar0);

            double distance01 = Distance(Point0ThroughNear0, Point1ThroughMiddleNear0);
            double distance12 = Distance(Point1ThroughMiddleNear0, Point2ThroughMiddleFar0);
            double distance23 = Distance(Point2ThroughMiddleFar0, Point3ThroughFar0);

            double d1 = distance01 / (distance01 + distance12);
            double d2 = distance12 / (distance12 + distance23);
            Point3D B1 = Mul(center01, center12, d1);
            Point3D B2 = Mul(center12, center23, d2);

            Vector3D B1_2Middle1 = Sub(Point1ThroughMiddleNear0, B1).ToVector3D();
            Vector3D B2_2Middle2 = Sub(Point2ThroughMiddleFar0, B2).ToVector3D();

            Point3D controlPoint1 = Add(center12, B1_2Middle1.ToPoint3D());
            Point3D controlPoint2 = Add(center12, B2_2Middle2.ToPoint3D());

            for (int i = 0; i < 10; i++)
            {
                Point3D x0 = Point1ThroughMiddleNear0;
                Point3D x1 = controlPoint1;
                Point3D x2 = controlPoint2;
                Point3D x3 = Point2ThroughMiddleFar0;


                double r;
                double a;
                double b;
                double c;
                double d;
                if (isHorizontal)
                {
                    r = Mul(Point2ThroughMiddleFar0, Point1ThroughMiddleNear0, (double)i / 10).X;
                    a = -x0.X + 3 * x1.X - 3 * x2.X + x3.X;
                    b = 3 * (x0.X - 2 * x1.X + x2.X);
                    c = 3 * (-x0.X + x1.X);
                    d = x0.X - r;
                }
                else
                {
                    r = Mul(Point2ThroughMiddleFar0, Point1ThroughMiddleNear0, (double)i / 10).Y;
                    a = -x0.Y + 3 * x1.Y - 3 * x2.Y + x3.Y;
                    b = 3 * (x0.Y - 2 * x1.Y + x2.Y);
                    c = 3 * (-x0.Y + x1.Y);
                    d = x0.Y - r;
                }

                double t = getXFrom1VariableCubicEquation(a, b, c, d);

                if (t >= 0 && t <= 1)
                {

                }
                else
                {
                    //MessageBox.Show("未找到合适的t值");
                    Debug.WriteLine("未找到合适的t值,t=" + t.ToString());
                }

                Point3D oneResult = CalcPoint(t, x0, x1, x2, x3);

                result.Add(oneResult);
            }
            return result;
        }

        public Point3D CalcPoint(double t, Point3D p0, Point3D p1, Point3D p2, Point3D p3)
        {
            Point3D result = new Point3D();
            result.X = bezierFormula(t, p0.X, p1.X, p2.X, p3.X);
            result.Y = bezierFormula(t, p0.Y, p1.Y, p2.Y, p3.Y);
            result.Z = bezierFormula(t, p0.Z, p1.Z, p2.Z, p3.Z);
            return result;
        }
        /// <summary>
        /// 三阶的贝塞尔曲线坐标计算公式:x=x0*pow(1-t,3)+3*x1*t*pow(1-t,2)+3*x2*pow(t,2)*(1-t)+x3*pow(t,3)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private double bezierFormula(double t, double x0, double x1, double x2, double x3)
        {
            return x0 * pow(1 - t, 3) + 3 * x1 * t * pow(1 - t, 2) + 3 * x2 * pow(t, 2) * (1 - t) + x3 * pow(t, 3);
        }

        private double sqrt3(double x)
        {
            return x >= 0 ? pow(x, 1.0 / 3) : -pow(abs(x), 1.0 / 3);
        }
        //算法来自于：https://baike.baidu.com/item/%E4%B8%80%E5%85%83%E4%B8%89%E6%AC%A1%E6%96%B9%E7%A8%8B/8388473?fr=aladdin
        /// <summary>
        /// 求一元三次方程的实数解(这种方法是系数为复数的情况的解法，我理解成了系数是实数了:(
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private double getXFrom1VariableCubicEquationOld(double a, double b, double c, double d)
        {
            if (a == 0)
            {
                return GetXFrom1VariableQuadraticEquation(b, c, d);
            }
            else
            {
                a = 1.0 / a;
                b *= a;
                c *= a;
                d *= a;
                double u = ((9.0 * c - 2.0 * b * b) * b - 27.0 * d) / 54.0;
                double v = 3.0 * ((4.0 * c - b * b) * c * c + ((4.0 * b * b - 18.0 * c) * b + 27.0 * d) * d);
                v = sqrt(v) / 18.0;
                double m = u + v;
                double n = u - v;
                if (abs(n) > abs(m))
                {
                    m = n;
                }
                a = b / -3.0;
                if (m != 0)
                {
                    m = sqrt3(m);
                    n = (b * b - 3.0 * c) / (9.0 * m);
                    return m + n + a;
                }
                return 0;
            }
        }

        /**假设一个一元三次方程为：ax³+bx²+cx+d=0（a≠0）
         *如果令x=z- b/3a 
         *（z是引入的一个未知数，只表示与x的关系，之所以这么令，是为了后面的可以更好地求出z来，然后再求x的值。）
         *原方程化为：a（z-b/3a）³+b（z-b/3a）²+c（z-b/3a）+d=0
         *a[z³-b³/27a³-3z*b/3a*（z-b/3a）]+b（z²+b²/9a²-2zb/3a）+c(z-b/3a)+d=0
         *整理有：
         *az³+(c -b²/3a)z+（2b³/27a² -bc/3a +d）=0
         *即：z³+(c/a -b²/3a²)z+（2b³/27a³ -bc/3a² +d/a）=0
         *令h=(c/a -b²/3a²)， g=（2b³/27a³ -bc/3a² +d/a）
         *方程为：z³+hz+g=0
         *上面就是特殊的一元三次方程，然后可以解出z的值来，再利用x=z-b/3a来求出x的值。
         *注意，上面的h，g只不过是为了表达方便，不用（a，b，c，d）写那么长而已。
         * CopyFrom:https://www.zybang.com/question/fa4cd44122eb701ab8c68ebe11d6048c.html
         */
        //算法CopyFrom：https://baike.baidu.com/item/%E4%B8%80%E5%85%83%E4%B8%89%E6%AC%A1%E6%96%B9%E7%A8%8B%E6%B1%82%E6%A0%B9%E5%85%AC%E5%BC%8F
        /// <summary>
        /// 求一元三次方程的实数解
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private double getXFrom1VariableCubicEquation(double a, double b, double c, double d)
        {
            if (a == 0)
            {
                return GetXFrom1VariableQuadraticEquation(b, c, d);
            }
            else
            {
                double p = (3 * a * c - b * b) / (3 * a * a);
                double q = (27 * a * a * d - 9 * a * b * c + 2 * b * b * b) / (27 * a * a * a);
                double delta = pow(q / 2, 2) + pow(p / 3, 3);
                if (delta > 0)
                {
                    double z1 = sqrt3(-q / 2 + sqrt(delta)) + sqrt3(-q / 2 - sqrt(delta));
                    double x1 = z1 - b / (3 * a);
                    return (x1 >= 0 && x1 <= 1) ? x1 : 0;
                }
                else if (delta == 0)
                {

                }
                else if (delta < 0)
                {
                    double r = sqrt(-pow(p / 3, 3));
                    double theta = arccos(-q / (2 * r)) / 3;
                    double xishu = 2 * sqrt3(r);
                    double z1 = xishu * cos(theta);
                    double z2 = xishu * cos(theta + pi / 180 * 120);
                    double z3 = xishu * cos(theta + pi / 180 * 240);
                    double x1 = z1 - b / (3 * a);
                    double x2 = z2 - b / (3 * a);
                    double x3 = z3 - b / (3 * a);

                    return (x1 >= 0 && x1 <= 1) ? x1 : ((x2 >= 0 & x2 <= 1) ? x2 : x3);
                }
                else
                {
                    return 0;
                }
                return 0;
            }
        }


    }
}
