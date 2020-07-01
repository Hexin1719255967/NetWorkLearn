using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCore
{
     public abstract class AbsHandlerCenter
    {
        /// <summary>
        /// 客户端连接
        /// </summary>
        /// <param name="token"></param>
        public abstract void ClientConnect(UserToken token);
        /// <summary>
        /// 收到客户端消息
        /// </summary>
        /// <param name="token"></param>
        /// <param name="model"></param>
        public abstract void MessageReceive(UserToken token,DataModel model);
        /// <summary>
        /// 客户端断开连接
        /// </summary>
        /// <param name="token"></param>
        /// <param name="error"></param>
        public abstract void ClientClose(UserToken token,string error);

    }
}
