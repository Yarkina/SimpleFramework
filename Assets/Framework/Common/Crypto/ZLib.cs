using System.Collections;
using System.Collections.Generic;
using System;

namespace Crypto
{
    public class ZLib
    {
        public static byte[] Zip(byte[] unzip)
        {
            var ziped = LZ4.LZ4Codec.Encode(unzip, 0, unzip.Length);
            var ulength = unzip.Length;
            var buffer = new byte[ziped.Length + 4];
            Buffer.BlockCopy(ziped, 0, buffer, 4, ziped.Length);
            buffer[0] = (byte)((ulength >> 24) & 0xFF);
            buffer[1] = (byte)((ulength >> 16) & 0xFF);
            buffer[2] = (byte)((ulength >> 8) & 0xFF);
            buffer[3] = (byte)((ulength >> 0) & 0xFF);
            //byte[] buf = BitConverter.GetBytes(ulength);
            //if (BitConverter.IsLittleEndian)
            //{
            //    Array.Reverse(buf);
            //}
            //Buffer.BlockCopy(buf, 0, buffer, 0, 4);
            return buffer;
        }
        public static byte[] UnZip(byte[] zip)
        {
            int length = (int)(((zip[0] << 24) | (zip[1] << 16) | (zip[2] << 8) | (zip[3] << 0)) & 0xFFFFFFFF);
            //byte[] buf = new byte[4];
            //Buffer.BlockCopy(zip, 0, buf, 0, 4);
            //if (BitConverter.IsLittleEndian) {
            //    Array.Reverse(buf);
            //}
            //int lenght = BitConverter.ToInt32(buf, 0);
            return LZ4.LZ4Codec.Decode(zip, 4, zip.Length - 4, length);
        }
    }
}
