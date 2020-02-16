using Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Helper
{
    sealed class NetHelper
    {
        const int PackHeadSize = 4;

        const byte CCC_Compress = 0x80;
        const byte CCC_Crypto = 0x40;
        const byte CCC_Crc = 0x20;
        static bool NetBigEndian = false;

        static byte CCC_Flag = CCC_Compress | CCC_Crypto | CCC_Crc;
        static bool Enabled_Compress { get { return (CCC_Flag & CCC_Compress) == CCC_Compress; } }
        static bool Enabled_Crypto { get { return (CCC_Flag & CCC_Crypto) == CCC_Crypto; } }
        static bool Enabled_Crc { get { return (CCC_Flag & CCC_Crc) == CCC_Crc; } }

        public static void Initialize(bool enableCompress = true, bool enableCrypto = true, bool enableCrc = true)
        {
            CCC_Flag = 0;
            if (enableCompress)
            {
                CCC_Flag |= CCC_Compress;
            }
            if (enableCrypto)
            {
                CCC_Flag |= CCC_Crypto;
            }
            if (enableCrc)
            {
                CCC_Flag |= CCC_Crc;
            }
        }

        public static void CheckReverse(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian == NetBigEndian)
            {
                Array.Reverse(bytes);
            }
        }
        public static byte[] Encode(byte[] arr, AesEncryptor aesEncryptor, byte sendIdx)
        {
            byte flag = sendIdx;
            int crc32 = 0;
            if (Enabled_Compress && false)
            {
                if (arr.Length >= 1024)
                {
                    arr = ZLib.Zip(arr);
                    flag |= CCC_Compress;
                }
            }
            if (Enabled_Crypto || true)
            {
                bool aesed = (new Random().Next() % 10) < 3;
                if (aesed || true)
                {
                    arr = aesEncryptor.Encrypt(arr);
                    flag |= CCC_Crypto;
                }
            }
            if (Enabled_Crc)
            {
                bool crced = (new Random().Next() % 10) < 3;
                if (crced)
                {
                    crc32 = Crc.Crc32(arr);
                    flag |= CCC_Crc;
                }
            }

            int alllen = arr.Length;// + 1 + 4;
            byte[] balllen = BitConverter.GetBytes(alllen);
            CheckReverse(balllen);
            
            var m2 = BitConverter.GetBytes(crc32);
            CheckReverse(m2);
            var os = new MemoryStream();
            os.Write(balllen, 0, PackHeadSize); //allLen
            //os.WriteByte(flag);//flag
            //os.Write(m2, 0, 4);//crc
            os.Write(arr, 0, arr.Length);
            return os.ToArray();
        }

        public static byte[] Decode(byte[] arr, AesDecryptor aesDecryptor, ref int recvIdx)
        {
            //byte flag = arr[0];
            //bool ziped = ((flag & 0x80) == 0x80);
            //bool aesed = ((flag & 0x40) == 0x40);
            //bool crced = ((flag & 0x20) == 0x20);
            //int idx = flag & 0x1F;
            //if (recvIdx == idx || true)
            {
                //recvIdx++;
                //if (recvIdx > 0x1F)
                //{
                //    recvIdx = 0;
                //}
                //var bcrc = new byte[4];
                //Buffer.BlockCopy(arr, 1, bcrc, 0, 4);
                //CheckReverse(bcrc);
                //int crc32 = BitConverter.ToInt32(bcrc, 0);
                byte[] data;
                //var data = new byte[arr.Length - 1 - 4];
                //Buffer.BlockCopy(arr, 1 + 4, data, 0, data.Length);
                //int ncrc32 = 0;
                //if (crced)
                //{
                //    ncrc32 = Crc.Crc32(data);
                //}
                //if (ncrc32 == crc32 || true)
                {
                    //if (aesed || true)
                    {
                        data = arr;
                        data = aesDecryptor.Decrypt(data);
                    }
                    //if (ziped && false)
                    //{
                    //    data = ZLib.UnZip(data);
                    //}
                    if (data != null)
                    {
                        return data;
                    }
                    else
                    {
                        throw new Exception("Recv Decode data null");
                    }
                }
                //else
                //{
                //    throw new Exception("Recv error crc32 " + crc32 + "   ncrc32" + ncrc32);
                //}
            }
            //else
            //{
            //    throw new Exception("Recv error idx " + idx + "   lidx" + recvIdx);
            //}
        }

        public static void SplitPack(ref byte[] recvBuffer, ref int receivedSize, ref int bufferSize, Action<byte[]> push)
        {
            //当前buf位置指针
            var offset = 0;
            var head = new byte[PackHeadSize];
            //在mBuffer中可能有多个逻辑数据包，逐个解出
            while (receivedSize - offset > PackHeadSize)
            {
                Buffer.BlockCopy(recvBuffer, offset, head, 0, PackHeadSize);
                CheckReverse(head);
                //解包大小
                var packSize = BitConverter.ToInt32(head, 0);
                if (receivedSize - offset - PackHeadSize >= packSize) //已经接收了一个完整的包
                {
                    //当前buf指针加下包头偏移
                    offset += PackHeadSize;

                    //包体大小
                    var pack = new byte[packSize];

                    //解MsgBody
                    Buffer.BlockCopy(recvBuffer, offset, pack, 0, packSize);
                    //当前buf指针加下Body偏移
                    offset += packSize;


                    //存起来
                    push(pack);
                }
                else if (bufferSize < packSize + PackHeadSize) //收到的包比buff大,需要做Buff的扩容
                {
                    //要扩容到的Buff大小
                    var newBuffSize = packSize + PackHeadSize;

                    //下面这段Baidu的 快速求 > newBuffSize 的 最小的2的幂次方数(原理近似快速的把最高为的1复制到右边所有的位置上然后+1)
                    newBuffSize |= (newBuffSize >> 1);
                    newBuffSize |= (newBuffSize >> 2);
                    newBuffSize |= (newBuffSize >> 4);
                    newBuffSize |= (newBuffSize >> 8);
                    newBuffSize |= (newBuffSize >> 16);
                    newBuffSize++;
                    if (newBuffSize < 0)
                    {
                        newBuffSize >>= 1;
                    }

                    var newBuff = new byte[newBuffSize];

                    //拷贝剩余的有效内容到新的buff
                    //Buffer中真正剩余的有效内容
                    receivedSize -= offset;
                    Buffer.BlockCopy(recvBuffer, offset, newBuff, 0, receivedSize);
                    bufferSize = newBuffSize;
                    recvBuffer = newBuff;
                    offset = 0;
                    break;
                }
                else //收到的包不完整 直接Break
                {
                    break;
                }
            }
            receivedSize -= offset;
            if (receivedSize > 0)
            {
                //buf内容前移
                Buffer.BlockCopy(recvBuffer, offset, recvBuffer, 0, receivedSize);
            }
        }

        public static IPAddress ParseIpAddressV6(string address)
        {
            IPAddress addrOut = null;
            if (IPAddress.TryParse(address, out addrOut))
            {
                return addrOut;
            }

            IPAddress[] addrList = Dns.GetHostAddresses(address);
            for (int i = 0; i < addrList.Length; i++)
            {
                addrOut = addrList[i];
                if (addrOut.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    break;
                }
            }

            return addrOut;
        }
    }
}
