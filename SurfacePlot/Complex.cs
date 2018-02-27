using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurfacePlot
{
    /// <summary>
    /// 复数类
    /// </summary>
    public class Complex
    {
        private double real;//实部
        private double image;//虚部
        /// <summary>
        /// 获取或设置实部
        /// </summary>
        public double Real
        {
            get { return real; }
            set { real = value; }
        }
        /// <summary>
        /// 获取或者设置虚部
        /// </summary>
        public double Image
        {
            get { return image; }
            set { image = value; }
        }
        public Complex(double real, double image)
        {
            this.real = real;
            this.image = image;
        }
        public Complex() { }
        /// <summary>
        /// 取共轭
        /// </summary>
        public Complex Conjugate()
        {
            //Complex complex = new Complex();
            //complex.real = this.real;
            //complex.image = -complex.image;
            //return complex;
            return new Complex(this.real, -this.image);
        }
        /// <summary>
        /// 加法重载函数
        /// </summary>
        /// <param name="C">加数</param>
        /// <param name="c">加数</param>
        /// <returns>复数相加的结果</returns>
        public static Complex operator +(Complex C, Complex c)
        {
            //Complex com = new Complex();
            //com.real = C.real + c.real;
            //com.image = C.image + c.image;
            //return com;
            return new Complex(c.real + C.real, C.image + c.image);
        }
        /// <summary>
        /// 复数的加法，可以同时实现多个复数相加
        /// 其实跟直接用+号来相加的结果是一样的，
        /// 个人只是想多学习可变参数的用法
        /// </summary>
        /// <param name="complexs"></param>
        /// <returns></returns>
        public Complex Add(params Complex[] complexs)
        {
            if (complexs.Length == 0)
            {
                throw new Exception("输入的参数不能为空！");
            }
            Complex com = new Complex();
            foreach (Complex c in complexs)
            {
                com = com + c;
            }
            return com;
        }
        /// <summary>
        /// 复数的减法重载函数
        /// </summary>
        /// <param name="C">被减数</param>
        /// <param name="c">减数</param>
        /// <returns>复数相减后的结果</returns>
        public static Complex operator -(Complex C, Complex c)
        {
            //Complex com = new Complex();
            //com.real = C.real -c.real;
            //com.image = C.image - c.image;
            //return com;
            return new Complex(C.real - c.real, C.image - c.Image);
        }
        /// <summary>
        /// 双等号函数的重载
        /// </summary>
        /// <param name="C"></param>
        /// <param name="c"></param>
        /// <returns>如果相等返回true，否则返回fasle</returns>
        public static bool operator ==(Complex C, Complex c)
        {
            return (C.real == c.real && C.image == c.image);
        }
        /// <summary>
        /// 不等号函数的重载
        /// </summary>
        /// <param name="C"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool operator !=(Complex C, Complex c)
        {
            return (C.real != c.real || C.image != c.image);
        }
        /// <summary>
        /// 复数的相减，可以同时实现多个复数相减
        /// 其实跟直接用-号来相加的结果是一样的，
        /// 个人只是想多学习可变参数的用法
        /// </summary>
        /// <param name="complexs">数的集合</param>
        /// <returns>相减操作后的复数</returns>
        public Complex Minus(params Complex[] complexs)
        {
            if (complexs.Length == 0)
            {
                throw new Exception("输入的参数不能为空！");
            }
            Complex com = complexs[0];
            for (int i = 1; i < complexs.Length; i++)
            {
                com = com - complexs[i];
            }
            return com;
        }
        /// <summary>
        /// 复数的乘法运算
        /// </summary>
        /// <param name="c"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        public static Complex operator *(Complex c, Complex C)
        {
            //(a+b*i)*(c+d*i)=(ac-bd)+(ad+bc)*i
            return new Complex(c.real * C.real - c.image * C.image, c.real * C.image + c.image * C.real);
        }
        public Complex Multiplicative(params Complex[] complexs)
        {
            if (complexs.Length == 0)
            {
                throw new Exception("输入的参数不能为空！");
            }
            Complex com = complexs[0];
            for (int i = 1; i < complexs.Length; i++)
            {
                com += complexs[i];
            }
            return null;
        }
        /// <summary>
        /// 复数除法
        /// </summary>
        /// <param name="C"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Complex operator /(Complex C, Complex c)
        {
            if (c.real == 0 && c.image == 0)
            {
                throw new Exception("除数的虚部和实部不能同时为零(除数不能为零)");
            }
            double real = (C.real * c.real + c.image * C.image) / (c.real * c.real + c.image + c.image);
            double image = (C.image * c.real - c.image * C.real) / (c.real * c.real + c.image + c.image);
            return new Complex(real, image);
        }
        /// <summary>
        /// 复数除法运算
        /// </summary>
        /// <param name="complexs">一系列复数</param>
        /// <returns>除法运算后的结果</returns>
        public Complex Divison(params Complex[] complexs)
        {
            if (complexs.Length == 0)
            {
                throw new Exception("输入的参数不能为空！");
            }
            foreach (Complex com in complexs)
            {
                if (com.image == 0 && com.real == 0)
                {
                    throw new Exception("除数的实部和虚部不能同时为零！");
                }
            }
            Complex COM = new Complex();
            COM = complexs[0];
            for (int i = 1; i < complexs.Length; i++)
            {
                COM = COM / complexs[i];
            }
            return COM;
        }
        /// <summary>
        /// 取模运算
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public double Mod(Complex c)
        {
            return Math.Sqrt(c.real * c.real + c.image * c.image);
        }
        /// <summary>
        /// 判断复数是否相等
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Complex)
            {
                Complex com = (Complex)obj;
                return (com.real == this.real && com.image == this.image);
            }
            return false;
        }
        /// <summary>
        /// 计算复数相位角
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static double GetAngle(Complex c)
        {
            return Math.Atan2(c.real, c.image);
        }
        public override string ToString()
        {
            //string str = null;
            //if (this.image == 0)
            //{
            //    str = "=";
            //}
            //else if (this.image > 0)
            //{
            //    str = ">";
            //}
            //switch (str)
            //{
            //    case ">":
            //        if (this.real == 0)
            //        {
            //            return string.Format("{0}i", this.image);
            //        }
            //        return string.Format("{0}+{1}i", this.real, this.image);
            //    case "=":
            //        return string.Format("{0}",this.real);
            //    default:
            //         if (this.real == 0)
            //        {
            //            return string.Format("{0}i", this.image);
            //        }
            //        return string.Format("{0}+{1}i", this.real, this.image);
            //}
            return string.Format("<{0} , {1}>", this.real, this.image);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
