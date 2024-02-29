using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json;
using Server.Database;
using System.Reflection.Metadata;
using Server.Database.Models;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Data.Sqlite;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Server
{
    //"Relogga" skapar 2 stycken av den nya
    //Ändra logga in knapp när inloggan(stäng av den, ändra den)
    //Fixa Online user så den uppdaterar när listan blir uppdaterad

    class Server
    {
        private static Server? instance = null;
        private TcpListener? TcpListener { get; set; } = null;
        private IPAddress Ip { get; set; }
        private int Port { get; set; }

        private readonly Dictionary<string, TcpClient> connectedClients = new();

        ApplicationDbContext db = new ApplicationDbContext();

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

        //public List<string> loggedIn = new List<string>();    //Gör som den checkar databasen istället
        private void HandleRequest(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            try
            {

                while (true)
                {
                    byte[] buffer = new byte[1024]; //1 KB
                    int recv = stream.Read(buffer, 0, buffer.Length);
                    if (recv == 0) break;

                    string request = Encoding.UTF8.GetString(buffer, 0, recv);

                    // Change string type from string to json
                    Request data = JsonSerializer.Deserialize<Request>(request);

                    switch (data.Type)
                    {
                        case "getallusers":
                            List<User> users;

                            users = db.Users.ToList();

                            string temp = "";
                            foreach (User user1 in users)
                            {
                                temp += user1.Username + ";";
                                temp += user1.Password + "/n";
                            }

                            Request message = new Request { Type = "message", Contents = new Message() { Contents = "success" } };

                            SendToClient(client, message);
							          break;
                            case "login":
                            User user = JsonSerializer.Deserialize<User>((JsonElement)data.Contents);

                            string inputname = user.Username;
                            string inputPassword = user.Password;

                            var check = db.Users.FirstOrDefault(u => u.Username == inputname && u.Password == inputPassword);

                            if (check != null)
                            {
                                //Checkar om det finns något i databasen med namnet och om det finns gå vidare
                                //OM DET FINNS
                                bool uniqueCheck = true;
                                if (connectedClients.ContainsKey(user.Username))
                                {
                                    uniqueCheck = false;
                                    break;
                                }

                                if (uniqueCheck)
                                {
                                    SendMessage(new Request() { Type = data.Type, Contents = user.Username }); //(fel här?)
                                    //loggedIn.Add(data.Sender);
                                    connectedClients.Add(user.Username, client);
                                }
                                else
                                {
                                    SendMessage(new Request() { Type = "error", Contents = user.Username });
                                }
                            }
                            else
                            {
                                //OM INTE FINNS
                                SendMessage(new Request() { Type = "error1", Contents = user.Username });
                            }
                            break;
                        case "register":
                            RegisterUser(data, client);
                            break;
                        case "message":
                            Message x = JsonSerializer.Deserialize<Message>((JsonElement)data.Contents);

                            Console.WriteLine(data.Contents.GetType().FullName);

                            if (x is Message m)
                            {
                                SendMessage(new Request() { Type = "message", Contents = new Message() { Contents = m.Contents, Sender = m.Sender, Recipient = m.Recipient } });
                                Console.Write(m.Contents);
                            }
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

        private void SendMessage(Request message)
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

        private void RegisterUser(Request data, TcpClient client)
        {
            try
            {
                User user = JsonSerializer.Deserialize<User>((JsonElement)data.Contents);

                // https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=visual-studio
                //Need to add logic to check if username is already taken or not in the database
                db.Add(new Database.Models.User { Username = user.Username, Password = user.Password });
                db.SaveChanges();

                Request message = new Request();

                message.Type = "register";
                message.Contents = "successful";

                SendToClient(client, message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SendToClient(TcpClient tcpClient, Request message)
        {
            NetworkStream stream = tcpClient.GetStream();

            string jsonString = JsonSerializer.Serialize(message);

            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }
    }
}