using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataModel
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public byte Type { get; set; }

    /// <summary>
    /// 请求类型
    /// </summary>
    public byte Request { get; set; }

    /// <summary>
    /// 消息体
    /// </summary>
    public byte[] Message { get; set; }

    public DataModel(byte type, byte request, byte[] message)
    {
        Type = type;
        Request = request;
        Message = message;
    }

    public DataModel() { }
}
