using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class UnityNetWorkManager : MonoBehaviour
{
    public string Ip;
    public int Port;

    public static UnityNetWorkManager Instance { get; set; }
    public Queue<byte[]> ReceoveDataQueue = new Queue<byte[]>();

    private void Awake()
    {
        Instance = this;
        //注册连接，断开，接收事件
        SocketClient.Instance.ConnectResultEvent += OnConnectResult;
        SocketClient.Instance.DataReceiveEvent += OnDataReceive;
        SocketClient.Instance.DisconnectEvent += OnDisConnect;
    }

    private void OnDisConnect()
    {
        Debug.Log("断开连接！");
    }

    private void OnDataReceive(byte[] data)
    {
        ReceoveDataQueue.Enqueue(data);
    }

    private void OnConnectResult(bool result)
    {
        Debug.Log("连接结果：" + result);
        if (result)
        {
            //开始发送心跳包
            StartCoroutine(SendHeart());
        }
    }
    /// <summary>
    /// 发送心跳包
    /// </summary>
    /// <returns></returns>
    IEnumerator SendHeart()
    {
        while (SocketClient.Instance.Connected)
        {
            yield return new WaitForSeconds(5);
            Send(new DataModel());
        }
    }
    public void Send(DataModel model)
    {
        if (!SocketClient.Instance.Connected)
        {
            Debug.Log("off line!");
            return;
        }
        byte[] message = model.Message;
        int messageLength = message == null ? 0 : message.Length;
        byte[] len = BitConverter.GetBytes(messageLength);
        //消息总长度=message包的长度+消息类型1+请求类型1+消息长度字节4
        byte[] buffer = new byte[messageLength + 2 + 4];
        //赋值消息长度
        Buffer.BlockCopy(len, 0, buffer, 0, 4);
        //赋值消息类型和请求类型
        byte[] code = new byte[2] { model.Type, model.Request };
        Buffer.BlockCopy(code, 0, buffer, 4, 2);
        //赋值消息包
        if (message!=null)
        {
            Buffer.BlockCopy(message, 0, buffer, 6, messageLength);
        }

    }

    public void Connect()
    {
        IPAddress iPAddress;
        if (!IPAddress.TryParse(Ip,out iPAddress)||Port<0||Port>65535)
        {
            Debug.LogError("ip和port是错误的！");
            return;
        }
        SocketClient.Instance.ConnectServer(Ip, Port);
    }

    public void DisConnect()
    {
        SocketClient.Instance.DisConnectServer();
    }
}
