using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokenRail3MonitorViaWiFi.Classes
{
    public class CalcIntFromBytes
    {
        /// <summary>
        /// 计算4字节表示的整数，大端模式
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="index">索引</param>
        /// <returns></returns>
        public static int CalcIntFrom4Bytes(byte[] data, int index)
        {
            if (index + 4 > data.Length)
            {
                throw new Exception("索引超出界限");
            }
            return (data[index] << 24) + (data[index + 1] << 16) + (data[index + 2] << 8) + data[index + 3];
        }

        /// <summary>
        /// 计算2字节表示的整数，大端模式
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="index">索引</param>
        /// <returns></returns>
        public static int CalcIntFrom2Bytes(byte[] data, int index)
        {
            if (index + 2 > data.Length)
            {
                throw new Exception("索引超出界限");
            }
            return (data[index] << 8) + data[index + 1];
        }

        public static byte[] Calc2BytesFromInt(int data)
        {
            byte[] result = new byte[2];
            result[0] = (byte)((data & 0xff00) >> 8);
            result[1] = (byte)(data & 0xff);
            return result;
        }


        public static byte[] Calc4BytesFromInt(int data)
        {
            byte[] result = new byte[4];
            result[0] = (byte)((data & 0xff000000) >> 24);
            result[1] = (byte)((data & 0xff0000) >> 16);
            result[2] = (byte)((data & 0xff00) >> 8);
            result[3] = (byte)(data & 0xff);
            return result;
        }
    }
}
