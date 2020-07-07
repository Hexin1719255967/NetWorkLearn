using NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpServer
{
    public class SocketServerManager
    {
        private HandlerCenter handlerCenter;
        private SocketServer socketServer;
        private HeartCheck heartCheck;
        //房间列表
        private List<Room> roomList = new List<Room>();
        //客户端列表
        private List<UserToken> clientList = new List<UserToken>();
        public List<UserToken> GetClientList ()
        {
            return clientList;
        }
        public void StartServer(int max,int port)
        {
            handlerCenter = new HandlerCenter(this);
            socketServer = new SocketServer(handlerCenter);
            heartCheck = new HeartCheck(20,this);
            //启动服务器
            socketServer.Start(max,port);
            Console.WriteLine("server start! maxclient:"+max+",port:"+port);
        }

        internal void ClientConnect(UserToken token)
        {
            clientList.Add(token);
        }

        internal void ClientClose(UserToken token)
        {
            clientList.Remove(token);
            if (token.Room != null)
            {
                LeaveRoom(token);
            }
        }


        /// <summary>
        /// 获取房间列表
        /// </summary>
        /// <returns></returns>
        public List<Room> GetRoomList()
        {
            return roomList;
        }

        internal void CloseClient(UserToken userToken)
        {
            socketServer.ClientClose(userToken,"heart out time!");
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="token"></param>
        public bool CreateRomm(string roomName, UserToken token)
        {
            Room room = new Room();
            room.RoomName = roomName;
            token.Room = room;
            room.UserList.Add(token);
            roomList.Add(room);
            return true;
        }
        /// <summary>
        /// 加入房间
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool JoinRoom(string roomName, UserToken token)
        {
            Room room = roomList.Find((Room r) => { return r.RoomName == roomName; });
            if (room != null)
            {
                token.Room = room;
                room.UserList.Add(token);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 离开房间
        /// </summary>
        /// <param name="token"></param>
        public void LeaveRoom(UserToken token)
        {
            Room room = token.Room;
            token.Room = null;
            room.UserList.Remove(token);
            //最后一个离开后，销毁房间
            if (room.UserList.Count == 0)
            {
                room.Clear();
                roomList.Remove(room);
            }
            //更新房间列表给所有客户端
            handlerCenter.OnRoomListUpdate();
        }
    }
}
