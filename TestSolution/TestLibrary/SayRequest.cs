using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace TestLibrary
{
    [Serializable]
    public class GenericMessage
    {
        public byte[] ToByteArray()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, this);
                return ms.ToArray();
            }
        }

        public static GenericMessage FromByteArray(byte[] data)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream(data))
            {
                return (GenericMessage)bf.Deserialize(ms);
            }
        }
    }

    /// <summary>
    /// 말하기 요청
    /// </summary>
    [Serializable]
    public class SayRequest : GenericMessage
    {   
        public string UserName { get; set; }        
        public string Message { get; set; }
        
    }
    /// <summary>
    /// 말하기 응답
    /// </summary>
    [Serializable]
    public class SayResponse : GenericMessage
    {        
        public string UserName { get; set; }
        
        public string Message { get; set; }
        
    }

}