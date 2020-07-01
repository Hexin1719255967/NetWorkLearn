using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCore
{
    public static class MessageCodec
    {

        public static byte[] Encode(DataModel model) 
        {
            byte[] message = model.Message;
            int messageLength = message == null ? 0 : message.Length;
            byte[] len = BitConverter.GetBytes(messageLength+2);
            //消息总长度=message包的长度+消息类型1+请求类型1+消息长度字节4
            byte[] buffer = new byte[messageLength + 2 + 4];
            //赋值消息长度
            Buffer.BlockCopy(len, 0, buffer, 0, 4);
            //赋值消息类型和请求类型
            byte[] code = new byte[2] { model.Type, model.Request };
            Buffer.BlockCopy(code, 0, buffer, 4, 2);
            //赋值消息包
            if (message != null)
            {
                Buffer.BlockCopy(message, 0, buffer, 6, messageLength);
            }
            return buffer;
        }
        public static DataModel Decode(byte[] data)
        {
            DataModel model = new DataModel();
            //消息类型
            model.Type = data[0];
            //请求类型
            model.Request = data[1];
            //包体数据
            if (data.Length > 2)
            {
                byte[] message= new byte[data.Length-2];
                Buffer.BlockCopy(data,2,message,0,data.Length-2);
                model.Message = message;
            }
            return model;
        }
    }
}
