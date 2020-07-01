using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetCore
{
    public class UserToken
    {
        SocketServer server;
        //用户连接后的Socket
        public Socket SocketClient;
        //用户异步接收
        public SocketAsyncEventArgs ReceiceSAEA;
        //用户异步发送
        public Queue<SocketAsyncEventArgs> SendSAEAQueue;

        public string UserName;
        public int UserId;
        public DateTime ConnectTime;
        public DateTime HeartTime;

        //是否处于使用状态
        public bool IsUsing;
        //数据接收缓存区
        List<byte> receiveBuffer = new List<byte>();
        //数据发送缓存区
        Queue<byte[]> sendQueue = new Queue<byte[]>();

        private AbsHandlerCenter handlerCenter;

        //用户的房间
        public Room Room;
        public UserToken(SocketServer s, AbsHandlerCenter center)
        {
            server=s;
            handlerCenter = center;
            IsUsing = false;
            ReceiceSAEA = new SocketAsyncEventArgs();
            ReceiceSAEA.Completed += ReceiceCompleted;
            ReceiceSAEA.SetBuffer(new byte[1024], 0, 1024);
            ReceiceSAEA.UserToken = this;

            SendSAEAQueue = new Queue<SocketAsyncEventArgs>();        
        }
        int sendCount;
        SocketAsyncEventArgs GetSendSAEA() 
        {
            if (SendSAEAQueue.Count == 0)
            {
                if (sendCount > 20) return null;

                SocketAsyncEventArgs send = new SocketAsyncEventArgs();
                send.Completed += SendCompleted;
                send.UserToken = this;
                sendCount++;
                return send;
            }
            else 
            {
                return SendSAEAQueue.Dequeue();
            }
        }

        private void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessSend(e);
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                //断开连接
                server.ClientClose(this,e.SocketError.ToString());
            }
            else
            {
                //消息发送成
                SendSAEAQueue.Enqueue(e);
                isSending = false;
                Send();
            }
        }
        /// <summary>
        /// 开始异步接收
        /// </summary>
        public void StartReceive()
        {
            if (SocketClient == null) return;
            if (!SocketClient.ReceiveAsync(ReceiceSAEA))
            {
                ProcessReceive(ReceiceSAEA);
            }
        }

        private void ReceiceCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                //BytesTransferred，获取在套接字操作中传输得字节数
                byte[] data = new byte[e.BytesTransferred];
                Buffer.BlockCopy(e.Buffer,0,data,0,e.BytesTransferred);
                Receive(data);
                StartReceive();
            }
            else
            {
                //断开连接
                server.ClientClose(this, e.SocketError.ToString());
            }
        }

        bool isReadingData;
        public void Receive(byte[] data)
        {
            HeartTime = DateTime.Now;
            receiveBuffer.AddRange(data);

            if (!isReadingData)
            {
                isReadingData = true;
                ReadData();
            }
        }
        /// <summary>
        /// 读取缓存区数据
        /// </summary>
        void ReadData()
        {
            if (receiveBuffer.Count < 4)
            {
                isReadingData = false;
                return;
            }
            byte[] lengthBytes = receiveBuffer.GetRange(0,4).ToArray();
            int length = BitConverter.ToInt32(lengthBytes,0);
            if (receiveBuffer.Count - 4 < length)
            {
                isReadingData = false;
                return;
            }
            byte[] data = receiveBuffer.GetRange(4,length).ToArray();

            lock (receiveBuffer)
            {
                receiveBuffer.RemoveRange(0, length + 4);
            }
            //将数据交到应用层处理
            DataModel model = MessageCodec.Decode(data);
            handlerCenter.MessageReceive(this, model);
            //递归处理
            isReadingData = false;
            ReadData();
        }

        public void WaitSend(byte[] data)
        {
            if (SocketClient == null)
            {
                return;
            }
            sendQueue.Enqueue(data);
            if (!isSending)
            {
                isSending = true;
                Send();
            }
        }
        bool isSending;
        void Send()
        {
            try
            {
                if (sendQueue.Count == 0) { isSending = false; return; }

                SocketAsyncEventArgs send=GetSendSAEA();
                if (send == null) return;

                byte[] data = sendQueue.Dequeue();
                if (data == null) return;


                send.SetBuffer(data, 0, data.Length);
                if (!SocketClient.SendAsync(send))
                {
                    ProcessSend(send);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("usertoken send error:" + e.Message + "," + e.StackTrace);
            }
        }
        public void Close()
        {
            IsUsing = false;
            sendQueue.Clear();
            receiveBuffer.Clear();
            isReadingData = false;
            isSending = false;
            SendSAEAQueue.Clear();

            try
            {
                SocketClient.Shutdown(SocketShutdown.Both);
                SocketClient.Dispose();
                SocketClient = null;
            }
            catch (Exception e)
            {
                Console.WriteLine("usertoken close error:"+e.Message+","+e.StackTrace);
            }

        }
    }
}
