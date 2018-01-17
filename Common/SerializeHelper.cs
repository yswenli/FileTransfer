using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace FileTransfer.Common
{
    public static class SerializeHelper
    {
        /// <summary>
        ///     二进制序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] ByteSerialize(object obj)
        {
            using (var m = new MemoryStream())
            {
                var bin = new BinaryFormatter();

                bin.Serialize(m, obj);

                return m.ToArray();
            }
        }

        /// <summary>
        ///     二进制反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static T ByteDeserialize<T>(byte[] buffer)
        {
            using (var m = new MemoryStream())
            {
                m.Write(buffer, 0, buffer.Length);
                m.Position = 0;

                var bin = new BinaryFormatter();

                return (T)bin.Deserialize(m);
            }
        }
    }
}
