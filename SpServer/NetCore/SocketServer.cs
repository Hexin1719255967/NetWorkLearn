using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetCore
{
    public class SocketServer
    {
        Socket server;
        int maxClient;
        List<UserToken> userToketnList;
        UserTokenPool pool;
        //当前连接的客户端数量
        int count;
        //连接信号量
        Semaphore acceptClientSp;
        private AbsHandlerCenter handlerCenter;

        public SocketServer(AbsHandlerCenter center)
        {
            handlerCenter = center;
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start(int max,int port)
        {
            maxClient = max;
            pool = new UserTokenPool(maxClient);
            userToketnList = new List<UserToken>(maxClient);
            acceptClientSp = new Semaphore(maxClient, maxClient);
            //初始化UserToken
            for (int i = 0; i < maxClient; i++)
            {
                UserToken token = new UserToken(this, handlerCenter);
                userToketnList.Add(token);
                pool.Push(token);
            }
            server.Bind(new IPEndPoint(IPAddress.Any,port));
            server.Listen(2);
            //开始接收客户端连接
            StartAccept(null);
        }

        private void StartAccept(SocketAsyncEventArgs e)
        {
            if (server == null) return;
            if (e == null)
            {
                e = new SocketAsyncEventArgs();
                e.Completed += AcceptCompleted;
            }
            else
            {
                e.AcceptSocket = null;
            }

            if (count >= maxClient)
            {
                Console.WriteLine("accept client is max!");
            }
            if (!server.AcceptAsync(e))
            {
                PrcessAccept(e);
            }
        }

        private void PrcessAccept(SocketAsyncEventArgs e)
        {
            if (server == null) return;
            if (count >= maxClient)
            {
                Console.WriteLine("accept client is waitting...!");
            }
            //信号量-1
            acceptClientSp.WaitOne();
            //连接数+1
            Interlocked.Add(ref count,1);
            Console.WriteLine("client num:"+count);
            UserToken token = pool.Pop();
            token.IsUsing = true;
            token.SocketClient = e.AcceptSocket;
            token.ConnectTime = DateTime.Now;
            token.HeartTime = DateTime.Now;
            token.UserName = "Temp-" + token.SocketClient.RemoteEndPoint;
            //通知应用层处理客户端连接
            handlerCenter.ClientConnect(token);
            //监听客户端的数据接收
            token.StartReceive();
            //监听新的用户连接
            StartAccept(e);
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            PrcessAccept(e);
        }

        public void ClientClose(UserToken userToken, string socketError)
        {
            if (userToken != null && userToken.IsUsing)
            {
                //通知应用层客户端断开连接
                handlerCenter.ClientClose(userToken,socketError);
                userToken.Close();
                //加回一个信号量
                acceptClientSp.Release();
                //归池
                pool.Push(userToken);
                Interlocked.Add(ref count,-1);
                Console.WriteLine("client num:"+count);
            }
        }
      
    }
}
