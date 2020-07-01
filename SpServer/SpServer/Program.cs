using NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int maxclient = 100;
            int port = 6650;
            SocketServerManager server = new SocketServerManager();
            server.StartServer(maxclient, port);
            Console.ReadLine();
        }
    }
}
