using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class SocketClient
{
    private static SocketClient _instance;
    public static SocketClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SocketClient();
            }
            return _instance;
        }
    }
    private Socket client;
    private IPEndPoint hostEndPoint;
    //线程同步
    private AutoResetEvent connetResultEvent;
    //异步接收对象
    private SocketAsyncEventArgs receiveAsync;
    //发送对象池
    private Stack<SocketAsyncEventArgs> sendAsyncPool;
    private int poolCapacity;
    //接收缓存区
    private List<byte> receiveBuffer;

    //连接结果委托
    public delegate void OnConnectResult(bool result);
    public OnConnectResult ConnectResultEvent;
    //断开服务器委托
    public delegate void OnDisconnect();
    public OnDisconnect DisconnectEvent;
    //接收数据委托
    public delegate void OnDataReceive(byte[] data);
    public OnDataReceive DataReceiveEvent;

    /// <summary>
    /// 是否连接服务器
    /// </summary>
    public bool Connected
    {
        get { return client != null && client.Connected; }
    }
    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    public void ConnectServer(string ip, int port)
    {
        if (Connected) return;
        //实例化hostEndPoint
        hostEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        //实例化socket
        client = new Socket(hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        //实现线程同步
        connetResultEvent = new AutoResetEvent(false);
        //实例化SocketAsyncEventArgs
        SocketAsyncEventArgs connectAsync = new SocketAsyncEventArgs();
        connectAsync.UserToken = client;
        connectAsync.RemoteEndPoint = hostEndPoint;
        connectAsync.Completed += ConnectResult;
        //实例化缓冲区
        receiveBuffer = new List<byte>();
        //实例化发送对象池
        sendAsyncPool = new Stack<SocketAsyncEventArgs>();
        //异步连接
        client.ConnectAsync(connectAsync);
        //阻塞主线程5秒
        connetResultEvent.WaitOne(5000);
        //连接结果委托调用
        ConnectResultEvent?.Invoke(Connected);
        if (Connected)
        {
            isConnected = true;
            //连接成功之后，实例化接收对象
            receiveAsync = new SocketAsyncEventArgs();
            receiveAsync.RemoteEndPoint = hostEndPoint;
            receiveAsync.Completed += ReceiveCompleted;
            byte[] buffer = new byte[1024];
            receiveAsync.SetBuffer(buffer, 0, buffer.Length);
            //开始异步接收
            if (!client.ReceiveAsync(receiveAsync))
            {
                processReceive(receiveAsync);
            }
        }
    }
    /// <summary>
    /// 服务器连接结果
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ConnectResult(object sender, SocketAsyncEventArgs e)
    {
        connetResultEvent.Set();
    }
    /// <summary>
    /// 处理接收结果
    /// </summary>
    /// <param name="e"></param>
    private void processReceive(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
        {
            byte[] data = new byte[e.BytesTransferred];
            Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
            lock (receiveBuffer)
            {
                receiveBuffer.AddRange(data);
            }
            //当缓存区数据大于4
            do
            {
                //读取前4个字节是包体的长度
                byte[] lengthBytes = receiveBuffer.GetRange(0, 4).ToArray();
                int dataLength = BitConverter.ToInt32(lengthBytes, 0);

                //判断缓存区剩余数据是否足够完整包体
                if (dataLength <= receiveBuffer.Count - 4)
                {
                    byte[] rev = receiveBuffer.GetRange(4, dataLength).ToArray();
                    receiveBuffer.RemoveRange(0, dataLength + 4);
                    //交到前端处理数据
                    DataReceiveEvent?.Invoke(rev);
                }
                else
                {
                    break;
                }

            } while (receiveBuffer.Count > 4);
            //是否挂起状态
            if (!client.ReceiveAsync(e))
            {
                processReceive(e);
            }
        }
        else
        {
            ProcessError(e);
        }
    }
    public void Send(byte[] data)
    {
        if (data == null || !Connected) return;
        SocketAsyncEventArgs async;
        if (sendAsyncPool.Count > 0)
        {
            lock (sendAsyncPool)
            {
                async = sendAsyncPool.Pop();
            }
        }
        else
        {
            //当异步发送对象大于100时停止实例新的对象
            if (poolCapacity >= 100) return;
            async = new SocketAsyncEventArgs();
            async.RemoteEndPoint = hostEndPoint;
            async.Completed += SendCompleted;
            poolCapacity++;
        }
        //写入发送数据
        async.SetBuffer(data, 0, data.Length);

        //异步发送，当I/O操作未挂起时调用ProcessSend
        if (!client.SendAsync(async))
        {
            ProcessSend(async);
        }
    }
    /// <summary>
    /// 处理发送结果
    /// </summary>
    /// <param name="e"></param>
    private void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            if (e != null)
            {
                lock (sendAsyncPool)
                {
                    sendAsyncPool.Push(e);
                }
            }
        }
        else
        {
            ProcessError(e);
        }
    }
    /// <summary>
    /// 发送完成时
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SendCompleted(object sender, SocketAsyncEventArgs e)
    {
        ProcessSend(e);
    }
    /// <summary>
    /// 接收完成时
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
    {
        processReceive(e);
    }
    private void ProcessError(SocketAsyncEventArgs e)
    {
        DisConnectServer();
    }

    bool isConnected;
    /// <summary>
    /// 断开服务器连接
    /// </summary>
    public void DisConnectServer()
    {
        if (isConnected)
        {
            isConnected = false;

            client.Dispose();
            foreach (var async in sendAsyncPool)
            {
                async.Completed -= SendCompleted;
            }
            sendAsyncPool.Clear();
            poolCapacity = 0;

            receiveAsync.Completed -= ReceiveCompleted;
            //断开服务器委托调用
            DisconnectEvent?.Invoke();
        }
    }
}


