using System;
using System.IO;
using System.Security.Cryptography;

namespace NII.Security.Services
{
    public static class CryptographyEngine
    {
        private static readonly string KeyPlaceholder = "__MASTER_CRYPTO_KEY__";
        private static byte[] _masterKey;

        private static byte[] MasterKey
        {
            get
            {
                if (_masterKey == null)
                {
                    _masterKey = Guid.Parse(KeyPlaceholder).ToByteArray();
                }
                return _masterKey;
            }
        }

        public static Tuple<byte[], byte[]> Encrypt(byte[] plainData)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = MasterKey;
                aes.GenerateIV();

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainData, 0, plainData.Length);
                        cs.FlushFinalBlock();
                    }
                    return Tuple.Create(ms.ToArray(), aes.IV);
                }
            }
        }

        public static byte[] Decrypt(byte[] encryptedData, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = MasterKey;
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedData, 0, encryptedData.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }
    }
}
