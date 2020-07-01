using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCore
{
    public class Room
    {
        public string RoomName;
        public byte[] AnchorData;
        public List<UserToken> UserList = new List<UserToken>();
        public void Clear()
        {
            RoomName = "";
            AnchorData = null;
            UserList.Clear();
        }
    }
}
