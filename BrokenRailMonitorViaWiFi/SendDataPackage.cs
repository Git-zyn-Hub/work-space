using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokenRail3MonitorViaWiFi
{
    public class SendDataPackage
    {
        private const byte _frameHeader1 = 0xAA;
        private const byte _frameHeader2 = 0x55;
        //private byte _length;
        //private byte _sourceAddress;
        //private byte _destinationAddress;
        //private byte _dataType;
        //private byte[] _dataContent;
        private static byte _checksum = 0;
        public SendDataPackage()
        {

        }


        public static byte[] PackageSendData(byte sourceAddr, byte destinationAddr, byte dataType, byte[] dataContent)
        {
            byte[] result;
            int length = 0;
            length = 7 + dataContent.Length;
            result = new byte[length];
            result[0] = _frameHeader1;
            result[1] = _frameHeader2;
            result[2] = (byte)length;
            result[3] = sourceAddr;
            result[4] = destinationAddr;
            result[5] = dataType;
            for (int i = 0; i < dataContent.Length; i++)
            {
                result[6 + i] = dataContent[i];
            }
            _checksum = 0;
            for (int i = 0; i < length - 1; i++)
            {
                _checksum += result[i];
            }
            result[length - 1] = _checksum;
            return result;
        }
        public static byte[] PackageSendData(Command3Type messageId, ConfigType mConfigType, int mConfigLength, byte[] mConfig)
        {
            byte[] result;
            int length = 0;
            length = 8 + mConfig.Length;
            result = new byte[length];
            result[0] = _frameHeader1;
            result[1] = _frameHeader2;
            result[2] = (byte)messageId;
            result[3] = (byte)mConfigType;
            result[4] = (byte)((mConfigLength & 0xff00) >> 8);
            result[5] = (byte)(mConfigLength & 0xff);
            for (int i = 0; i < mConfig.Length; i++)
            {
                result[6 + i] = mConfig[i];
            }
            int checksum = 0;
            for (int i = 2; i < length - 2; i++)
            {
                checksum += result[i];
            }
            result[length - 2] = (byte)((checksum & 0xff00) >> 8);
            result[length - 1] = (byte)(checksum & 0xff);
            return result;
        }
    }
}
