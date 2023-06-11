using Google.Protobuf;
using Messages;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;


namespace Library.Helper.Encrypt
{
    //Aes 객체를 싱글턴으로 사용하는 것은 일반적으로 권장되지 않습니다.
    //그 이유는 아래와 같습니다:
    //1. Thread-safety: Aes 클래스는 thread-safe 하지 않습니다.
    //즉, 한 번에 하나의 스레드만이 Aes 객체를 안전하게 사용할 수 있습니다. 
    //따라서 여러 스레드에서 동시에 Aes 객체를 사용하려면 동기화 메커니즘을 적절하게 적용해야 합니다.
    //2. Key and IV management: 암호화 키(Key) 와 초기화 벡터(IV) 는 보통 각각의 암호화 작업에 대해
    // 새롭게 생성됩니다.
    //이는 Aes 객체가 재사용될 때마다 Key와 IV 값을 적절하게 업데이트해야 함을 의미합니다. 
    //이러한 관리가 제대로 이루어지지 않으면 보안상의 위험이 발생할 수 있습니다.
    //3. Resource management: 암호화 작업은 CPU 리소스를 상당히 사용합니다. 
    //따라서 Aes 객체를 지속적으로 재사용하면 시스템 리소스가 과도하게 소모될 수 있습니다.
    //따라서 Aes 객체는 일반적으로 using 문을 사용하여 필요한 동안만 생성하고 사용한 후
    //바로 소멸시키는 것이 권장됩니다.
    //이 방식은 리소스 관리를 효율적으로 하면서도 Aes 객체의 생명 주기를 적절하게 관리할 수 있습니다.
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
            byte[] decrypted;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;
                aesAlg.IV = _iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(data))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            csDecrypt.CopyTo(ms);
                            decrypted = ms.ToArray();
                        }
                    }
                }
            }

            return decrypted;
        }

        public static void Test()
        {
            var response = new MessageWrapper
            {
                SayResponse = new SayResponse
                {
                    UserId = "test1",
                    Message = "한글한글"
                }
            };

            // Your original data
            //byte[] original = { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1 };                
            var original = response.ToByteArray();
            int buffSize = original.Length;            

            // Encrypt and decrypt the data
            byte[] encrypted = CryptographyHelper.EncryptData(original);
            byte[] decrypted = CryptographyHelper.DecryptData(encrypted, buffSize);

            var originalDecrupt = MessageWrapper.Parser.ParseFrom(original);
            var decrptedWrapper = MessageWrapper.Parser.ParseFrom(decrypted);

            // Output decrypted data
            foreach (byte b in decrypted)
                Console.Write(b + " ");  // Should output 1 2 3 4 5
        }
    }
}
