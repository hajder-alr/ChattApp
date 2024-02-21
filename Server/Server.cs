using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

class Server
{
    private static Server? instance = null;
    private TcpListener? tcpListener { get; set; } = null;
    private IPAddress _ip { get; set; }
    private int _port { get; set; }

    private Server(string ip, int port)
    {
        _ip = IPAddress.Parse(ip);
        _port = port;
    }

    public static Server GetInstance(string ip = "127.0.0.1", int port = 1302)
    {
        if (instance == null)
        {
            instance = new Server(ip, port);
        }
        return instance;
    }

    public void InitializeServer()
    {
        if (tcpListener == null)
        {
            tcpListener = new TcpListener(_ip, _port);
            tcpListener.Start();
        }

        while (true)
        {
            Console.WriteLine("waiting for connection..");
            TcpClient client = tcpListener.AcceptTcpClient();
            Console.WriteLine("Client accepted.");

            NetworkStream stream = client.GetStream();
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024]; //1 KB
                    int recv = stream.Read(buffer, 0, buffer.Length);
                    if (recv == 0) break;
                    string request = Encoding.UTF8.GetString(buffer, 0, recv);
                    Console.WriteLine("request received: " + request);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong.");
                Console.WriteLine(e.ToString());
            }
        }
    }
}