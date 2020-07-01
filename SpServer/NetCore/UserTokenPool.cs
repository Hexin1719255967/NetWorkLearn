using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCore
{
     public class UserTokenPool
    {
        private Stack<UserToken> pool;
        public UserTokenPool(int max)
        {
            pool = new Stack<UserToken>(max);
        }
        /// <summary>
        /// 取出一个连接对象
        /// </summary>
        /// <returns></returns>
        public UserToken Pop()
        {
            return pool.Pop();
        }
        /// <summary>
        /// 插入一个连接对象
        /// </summary>
        /// <param name="token"></param>
        public void Push(UserToken token)
        {
            if (token != null)
            {
                pool.Push(token);
            }
        }
        public void Clear()
        {
            pool.Clear();
        }
        public int Size
        {
            get { return pool.Count; }
        }
    }
}
