using NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataProtocol;
namespace SpServer
{
    public class HandlerCenter : AbsHandlerCenter
    {
         SocketServerManager socketServerManager;
        public HandlerCenter(SocketServerManager s)
        {
            socketServerManager = s;
        }
        public override void ClientClose(UserToken token, string error)
        {
            Console.WriteLine("client close:"+token.UserName+","+error);
            socketServerManager.ClientClose(token);
        }

        public override void ClientConnect(UserToken token)
        {
            Console.WriteLine("client connect:" + token.UserName );
            socketServerManager.ClientConnect(token); 
        }
        public override void MessageReceive(UserToken token, DataModel model)
        {
            switch (model.Type)
            {
                case DataType.TYPE_NONE:
                    break;
                case DataType.TYPE_SPORDER:
                    SpHandler(token,model);
                    break;
                default:
                    break;
            }

        }

        private void SpHandler(UserToken token, DataModel model)
        {
            switch (model.Request)
            {
                case DataRequest.UPDATE_NAME_C:
                    UpdatName(token, model);
                    break;
                case DataRequest.GET_ROOMLIST_C:
                    GetRoomList(token, model);
                    break;
                case DataRequest.ROOM_CREATE_C:
                    CreateRoom(token, model);
                    break;
                case DataRequest.ROOM_JOIN_C:
                    JoinRoom(token, model);
                    break;
                case DataRequest.ROOM_LEAVE_C:
                    LeaveRoom(token, model);
                    break;
                case DataRequest.ROOM_UPLOAD_ANCHOR_C:
                    UploadAnchor(token, model);
                    break;
                case DataRequest.ROOM_DOWNLOAD_ANCHOR_C:
                    DownloadAnchor(token, model);
                    break;         
                case DataRequest.ROOM_SEND_ALL:
                    SendToRoomAll(token, model); 
                    break;
                case DataRequest.ROOM_SEND_OTHER:
                    SendToRoomOther(token, model);
                    break;
                default:
                    break;
            }
        }
        void Send(UserToken token, DataModel model)
        {
            Send(token,model.Type,model.Request,model.Message);
        }
        void Send(UserToken token,byte type,byte request,byte[]message)
        {
            DataModel model = new DataModel(type, request,message);
            byte[] data = MessageCodec.Encode(model);
            token.WaitSend(data);
        }
        private void UpdatName(UserToken token, DataModel model)
        {
            string username = Encoding.UTF8.GetString(model.Message);
            token.UserName = username;
            Send(token,DataType.TYPE_SPORDER,DataRequest.UPDATE_NAME_S,BitConverter.GetBytes((int)ResultCode.RESULT_SUCCESS));
            Console.WriteLine("-->update name:"+token.UserName);
        }

        /// <summary>
        /// 通知所有用户房间列表有更新，发送新的房间列表
        /// </summary>
        internal void OnRoomListUpdate()
        {
            RoomList list = new RoomList();
            for (int i = 0; i < socketServerManager.GetRoomList().Count; i++)
            {
                string str = socketServerManager.GetRoomList()[i].RoomName + "," + socketServerManager.GetRoomList()[i].UserList.Count;
                list.List.Add(str);
            }
            DataModel model = new DataModel();
            model.Type = DataType.TYPE_SPORDER;
            model.Request = DataRequest.GET_ROOMLIST_S;
            model.Message = DataCodec.TobyteArray(list);

            for (int i = 0; i < socketServerManager.GetClientList().Count; i++)
            {
                Send(socketServerManager.GetClientList()[i], model);
            }
        }

        private void GetRoomList(UserToken token, DataModel model)
        {
            RoomList list = new RoomList();
            for (int i = 0; i < socketServerManager.GetRoomList().Count; i++)
            {
                string str = socketServerManager.GetRoomList()[i].RoomName + "," + socketServerManager.GetRoomList()[i].UserList.Count;
                list.List.Add(str);
            }
            model.Request = DataRequest.GET_ROOMLIST_S;
            model.Message = DataCodec.TobyteArray(list);
            Send(token, model);
            Console.WriteLine(token.UserName + "--> get roomlist:"+ list.List.Count);
        }

        private void CreateRoom(UserToken token, DataModel model)
        {
            if (token.Room == null)
            {
                string roomName = Encoding.UTF8.GetString(model.Message);
                if (string.IsNullOrEmpty(roomName))
                {
                    Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_CREATE_S, BitConverter.GetBytes((int)ResultCode.RESULT_FAIL));
                    Console.WriteLine(token.UserName + "--> create room fail!");
                    return;
                }
                //房间名是否重复
                for (int i = 0; i < socketServerManager.GetRoomList().Count; i++)
                {
                    if (socketServerManager.GetRoomList()[i].RoomName == roomName)
                    {
                        Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_CREATE_S, BitConverter.GetBytes((int)ResultCode.RESULT_FAIL));
                        Console.WriteLine(token.UserName + "--> create room fail!");
                        return;
                    }
                }
                if (socketServerManager.CreateRomm(roomName, token))
                {
                    //通知创建者创建成功
                    Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_CREATE_S, BitConverter.GetBytes((int)ResultCode.RESULT_SUCCESS));
                    Console.WriteLine(token.UserName + "--> create a room:"+roomName);
                    //房间更新
                    OnRoomListUpdate();
                }
            }
            else
            {
                Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_CREATE_S, BitConverter.GetBytes((int)ResultCode.RESULT_INROOM));
                Console.WriteLine(token.UserName+"--> is already in room!");
            }
        }
        private void JoinRoom(UserToken token, DataModel model)
        {
            if (token.Room == null)
            {
                string roomName = Encoding.UTF8.GetString(model.Message);
                if (socketServerManager.JoinRoom(roomName, token))
                {
                    Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_CREATE_S, BitConverter.GetBytes((int)ResultCode.RESULT_SUCCESS));
                    //房间更新
                    OnRoomListUpdate();

                    Console.WriteLine(token.UserName + "--> join room:" + roomName);
                }
                else
                {
                    Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_CREATE_S, BitConverter.GetBytes((int)ResultCode.RESULT_FAIL));
                    Console.WriteLine(token.UserName + "--> join room not find:" + roomName);
                }
            }
            else
            {
                Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_CREATE_S, BitConverter.GetBytes((int)ResultCode.RESULT_INROOM));
                Console.WriteLine(token.UserName + "--> is already in room!");
            }
        }
        private void LeaveRoom(UserToken token, DataModel model)
        {
            if (token.Room != null)
            {
                socketServerManager.LeaveRoom(token);
                Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_LEAVE_S, BitConverter.GetBytes((int)ResultCode.RESULT_SUCCESS));
                Console.WriteLine(token.UserName + "--> leave room!");
            }
            else
            {
                Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_LEAVE_S, BitConverter.GetBytes((int)ResultCode.RESULT_NOTINROOM));
                Console.WriteLine(token.UserName + "--> is not in room!");
            }
        }
        private void UploadAnchor(UserToken token, DataModel model)
        {
            if (token.Room != null)
            {
                token.Room.AnchorData = model.Message;
                Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_UPLOAD_ANCHOR_S, BitConverter.GetBytes((int)ResultCode.RESULT_SUCCESS));
                Console.WriteLine(token.UserName + "--> upload anchor:"+ token.Room.AnchorData.Length);
            }
            else
            {
                Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_UPLOAD_ANCHOR_S, BitConverter.GetBytes((int)ResultCode.RESULT_NOTINROOM));
                Console.WriteLine(token.UserName + "--> is not in room!");
            }
        }
        private void DownloadAnchor(UserToken token, DataModel model)
        {
            if (token.Room != null)
            {
                if (token.Room.AnchorData != null)
                {
                    Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_DOWNLOAD_ANCHOR_S, token.Room.AnchorData);
                    Console.WriteLine(token.UserName + "--> download anchor:" + token.Room.AnchorData.Length);
                }
                else
                {
                    Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_DOWNLOAD_ANCHOR_S, BitConverter.GetBytes((int)ResultCode.RESULT_NOANCHOR));
                    Console.WriteLine(token.UserName + "--> room is no anchor!");
                }
            }
            else
            {
                Send(token, DataType.TYPE_SPORDER, DataRequest.ROOM_DOWNLOAD_ANCHOR_S, BitConverter.GetBytes((int)ResultCode.RESULT_NOTINROOM));
                Console.WriteLine(token.UserName + "--> is not in room!");
            }
        }
        private void SendToRoomAll(UserToken token, DataModel model)
        {
            if (token.Room != null)
            {
                for (int i = 0; i < token.Room.UserList.Count; i++)
                {
                    Send(token.Room.UserList[i], model);
                }
            }
        }
        private void SendToRoomOther(UserToken token, DataModel model)
        {
            if (token.Room != null) 
            {
                for (int i = 0; i < token.Room.UserList.Count; i++)
                {
                    if(token.Room.UserList[i]!=token)
                    Send(token.Room.UserList[i], model);
                }
            }
        }

    }

}
