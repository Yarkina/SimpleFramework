using Crypto;
using Helper;
using Net.Server;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Net.Common
{
    internal class AesKeyIV
    {
        const int KeyIVLen = 16;
        internal static byte[] GenKeyIV()
        {
            var bytes = new byte[KeyIVLen];
            var rand = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)rand.Next(0, 10);
            }
            return bytes;
        }
        internal static bool Check(byte[] bytes)
        {
            return bytes != null && bytes.Length == KeyIVLen;
        }
        internal static byte[] PraseKey(byte[] bytes)
        {
            var key = new byte[KeyIVLen];
            Buffer.BlockCopy(bytes, 0, key, 0, key.Length);
            return key;
        }
        internal static byte[] PraseIV(byte[] bytes)
        {
            var iv = new byte[KeyIVLen];
            Buffer.BlockCopy(bytes, KeyIVLen, iv, 0, iv.Length);
            return iv;
        }
        internal static void SendAesKeyIVRsa(Socket sock, string rsaPub, byte[] kiv)
        {
            if (!Rsa.Encrypt(rsaPub, kiv, out kiv))
            {
                sock.Close();
                throw new Exception("Rsa Encrypt error");
            }
            byte[] len = BitConverter.GetBytes(kiv.Length);
            NetHelper.CheckReverse(len);
            sock.Send(len);
            sock.Send(kiv);
        }
        internal static void SendAesKeyIVAes(Socket sock, byte[] aesKIV, byte[] kiv)
        {
            var encrypt = new AesEncryptor(aesKIV, aesKIV);
            kiv = encrypt.Encrypt(kiv);
            byte[] len = BitConverter.GetBytes(kiv.Length);
            NetHelper.CheckReverse(len);
            sock.Send(len);
            sock.Send(kiv);
        }
    }
}
