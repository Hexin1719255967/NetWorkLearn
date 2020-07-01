
namespace DataProtocol
{
    public class DataRequest
    {
        //客户端上传用户名
        public const byte UPDATE_NAME_C = 1;
        public const byte UPDATE_NAME_S = 2;
        //获取房间列表
        public const byte GET_ROOMLIST_C = 3;
        public const byte GET_ROOMLIST_S = 4;
        //创建房间
        public const byte ROOM_CREATE_C = 10; 
        public const byte ROOM_CREATE_S = 11;
        //上传锚点
        public const byte ROOM_UPLOAD_ANCHOR_C = 12;
        public const byte ROOM_UPLOAD_ANCHOR_S = 13;
        //加入房间
        public const byte ROOM_JOIN_C = 14;
        public const byte ROOM_JOIN_S = 15;
        //下载锚点
        public const byte ROOM_DOWNLOAD_ANCHOR_C = 16;
        public const byte ROOM_DOWNLOAD_ANCHOR_S = 17;
        //离开房间
        public const byte ROOM_LEAVE_C = 18;
        public const byte ROOM_LEAVE_S = 19;
        //向房间内所有用户发送数据
        public const byte ROOM_SEND_ALL = 20; 
        //向房间内除自己外的所有用户发送数据
        public const byte ROOM_SEND_OTHER = 21;
    }
}
