using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Crypto
{
    public class AesEncryptor
    {
        AesManaged m_AesManager;
        ICryptoTransform m_AesEncryptor;
        public AesEncryptor(byte[] key, byte[] iv)
        {
            m_AesManager = new AesManaged { Mode = CipherMode.ECB, Padding = PaddingMode.None };
            m_AesEncryptor = new CounterModeCryptoTransform(m_AesManager, key, iv);
        }
        public byte[] Encrypt(byte[] data, int startPos = 0)
        {
            return m_AesEncryptor.TransformFinalBlock(data, startPos, data.Length - startPos);
        }
    }
}