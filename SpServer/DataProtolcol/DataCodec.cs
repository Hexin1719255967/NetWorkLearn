using System;
using System.IO;
using System.Xml.Serialization;

namespace DataProtocol
{
    public static class DataCodec
    {
        /// <summary>
        /// 序列化类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static byte[] TobyteArray<T>(T t)
        {
            if (t == null) return null;
            using (MemoryStream memorry=new MemoryStream ())
            {
                try
                {
                    new XmlSerializer(typeof(T)).Serialize(memorry, t);
                    return memorry.ToArray();
                }
                catch (Exception e)
                {
                    //Console.WriteLine("TobyteArray error:" + e.Message);
                    return null;
                }
            }
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T ToObject<T>(byte[] data)
        {
            if (data == null) return default(T);
            using (MemoryStream memorry = new MemoryStream(data))
            {
                try
                {
                    return (T)(new XmlSerializer(typeof(T)).Deserialize(memorry));
                }
                catch (Exception e)
                {
                    //Console.WriteLine("TobyteArray error:" + e.Message);
                    return default(T);
                }
            }
        }
    }
}
