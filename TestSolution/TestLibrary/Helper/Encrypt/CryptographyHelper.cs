using System;
using System.IO;
using System.Security.Cryptography;


namespace TestLibrary.Helper.Encrypt
{
    public static class CryptographyHelper
    {
        // key, iv값
        private static readonly byte[] _key = {
            0x73,0x50,0x44,0x4e,0x01,0xc1,0x28,0x64,0xfa,0xa2,
            0xc4,0xda,0xf0,0xb6,0xdc,0xae,0x29,0xc6,0x92,0xb8,
            0x62,0x2c,0x5b,0xaa,0xaa,0xb9,0x12,0x9f,0xca,0x59,
            0xec,0xa3,
        };
        private static readonly byte[] _iv = {
            0xc5,0x49,0xfe,0x7a,0xf3,0xa5,0xeb,0xca,
            0xd5,0xcf,0x67,0xba,0x6f,0x43,0x9e,0xbb,
        };
        
        /// <summary>
        /// 암호화 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] EncryptData(byte[] data)
        {
            byte[] encrypted;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;
                aesAlg.IV = _iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }
            return encrypted;
        }

        /// <summary>
        /// 암호화된 패킷 복화화
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>

        public static byte[] DecryptData(byte[] data, int originSize)
        {            
            byte[] destination = null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;
                aesAlg.IV = _iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(data))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        var decrypted = new byte[data.Length];
                        csDecrypt.Read(decrypted, 0, decrypted.Length);

                        if(decrypted.Length > 0)
                        {
                            destination = new byte[originSize]; // 새 배열
                            Buffer.BlockCopy(decrypted, 0, destination, 0, originSize);
                        }
                        else
                        {
                            destination = Array.Empty<byte>();
                        }
                    }
                }
            }
            return destination;
        }
    }
}
