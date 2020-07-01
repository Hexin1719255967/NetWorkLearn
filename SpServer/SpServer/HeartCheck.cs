using NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpServer
{
    public class HeartCheck
    {
        private SocketServerManager socketServerManager;
        private Thread heartThread;
        int heartOutTime;
        public HeartCheck(int time, SocketServerManager s)
        {
            heartOutTime = time;
            socketServerManager = s;
            StartCheck();
        }

        private void StartCheck()
        {
            Console.WriteLine("StartCheck...");
            startCheck = true;
            heartThread = new Thread(ThreadStart);
            heartThread.Start();
        }

        bool startCheck;
        private void ThreadStart()
        {
            while (heartThread.IsAlive)
            {
                if (socketServerManager.GetClientList().Count > 0)
                {
                    UserToken[] userList = socketServerManager.GetClientList().ToArray();
                    for (int i = 0; i < userList.Length; i++)
                    {
                        //心跳超时
                        if ((DateTime.Now - userList[i].HeartTime).TotalSeconds > heartOutTime)
                        {
                            Console.WriteLine("Heart out time:"+ userList[i].UserName);
                            //关闭客户端
                            socketServerManager.CloseClient(userList[i]);
                        }
                    }
                }
                //每10秒检测一次
                for (int i = 0; i < 10; i++)
                {
                    if (!startCheck) return;
                    Thread.Sleep(1000);
                }
            }
        }
        public void Close()
        {
            startCheck = false;
            Console.WriteLine("Heart check stop!");
        }
    }
}
