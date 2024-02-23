using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    internal class Program
    {
        public static void Main()
        {
            Server.GetInstance().InitializeServer();
        }
    }
}