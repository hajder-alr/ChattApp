﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json;

class Server
{
    private static Server? instance = null;
    private TcpListener? TcpListener { get; set; } = null;
    private IPAddress Ip { get; set; }
    private int Port { get; set; }

    private readonly Dictionary<string, TcpClient> connectedClients = new();

    private Server(string _ip, int _port)
    {
        Ip = IPAddress.Parse(_ip);
        Port = _port;
    }

    public static Server GetInstance(string ip = "127.0.0.1", int port = 1302)
    {
        if (instance == null)
        {
            string pi = IPAddress.Any.ToString();
            instance = new Server(pi, port);
            Console.Write(pi);
        }
        return instance;
    }

    public void InitializeServer()
    {
        if (TcpListener == null)
        {
            TcpListener = new TcpListener(Ip, Port);
            TcpListener.Start();
        }

        while (true)
        {
            Console.WriteLine("waiting for connection..");

            TcpClient client = TcpListener.AcceptTcpClient();

            Console.WriteLine("Client accepted.");

            Task.Run(() => HandleRequest(client));
        }
    }

    private void HandleRequest(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        try
        {
            while (true)
            {
                byte[] buffer = new byte[1024]; 
                int recv = stream.Read(buffer, 0, buffer.Length);

                if (recv == 0) { break; }

                string request = Encoding.UTF8.GetString(buffer, 0, recv);
                Message data = JsonSerializer.Deserialize<Message>(request);

                if (data != null) 
                {
                    if (!connectedClients.ContainsKey(data.Sender))
                        connectedClients.Add(data.Sender, client);
                }

                switch (data.Type)
                {
                    case "login":
                        SendMessage(new Message() { MessageContents = data.MessageContents, Sender = data.Sender });
                        break;
                    case "register":
                        break;
                    case "message":
                        SendMessage(new Message() { MessageContents = data.MessageContents, Sender = data.Sender });
                        break;
                    default:
                        break;
                }
                Console.WriteLine("request received: " + request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong.");
            Console.WriteLine(e.ToString());
        }
    }

    private void SendMessage(Message message)
    {
        foreach (var tcp in connectedClients)
        {
            TcpClient client = tcp.Value;
            NetworkStream stream = client.GetStream();

            string jsonString = JsonSerializer.Serialize(message);

            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }
    }
}
