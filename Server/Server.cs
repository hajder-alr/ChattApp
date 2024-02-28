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
    //
    class Server
    {
        string inputname = "dennis";
        string query = "SELECT Username FROM User WHERE Username=inputname";
        
        //
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

		public List<string> loggedIn = new List<string>();    //Gör som den checkar databasen istället
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
                    Message data = JsonSerializer.Deserialize<Message>(request);

                    if (data != null) // User gets added when sending first request
                    {
                        if (!connectedClients.ContainsKey(data.Sender))
                            connectedClients.Add(data.Sender, client);
                    }

                    switch (data.Type)
                    {
                        case "getallusers":
                            List<User> users;

                            users = db.Users.ToList();

                            string temp = "";
                            foreach (User user in users)
                            {
                                temp += user.Username + ";";
                                temp += user.Password + "/n";
                            }

                            Message message = new Message { MessageContents = temp };
                            SendToClient(client, message);
							break;
                        case "login":
							string inputname = data.Sender;
							using (var dbContext = new ApplicationDbContext())
							{
								using (var ApplicationDbContext = new ApplicationDbContext())
								{
									var user = dbContext.Users.FirstOrDefault(u => u.Username == inputname);
									if (user != null)
									{
                                        //Checkar om det finns något i databasen med namnet och om det finns gå vidare
										//OM DET FINNS
										bool uniqueCheck = true;
										foreach (string online in loggedIn)
										{
											if (online.Contains(data.Sender))    //Ändra som den checkar databas
											{
												uniqueCheck = false;
												break;
											}
										}
										if (uniqueCheck)
										{
											SendMessage(new Message() { Type = data.Type, Sender = data.Sender });
											loggedIn.Add(data.Sender);
										}
										else
										{
											SendMessage(new Message() { Type = "error", Sender = data.Sender });
										}


									}
									else
									{
										//OM INTE FINNS
										SendMessage(new Message() { Type = "error1", Sender = data.Sender });
									}
								}
							}
							
							break;
                        case "register":
                            RegisterUser(data, client);
                            break;
                        case "message":
                            SendMessage(new Message() { Type = data.Type, MessageContents = data.MessageContents, Sender = data.Sender });
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

        private void RegisterUser(Message data, TcpClient client)
        {
            try
            {
                // https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=visual-studio
                db.Add(new Database.Models.User { Username = data.Username, Password = data.Password });
                db.SaveChanges();

                Message message = new Message();
                message.Type = "register";
                message.MessageContents = "Success";
                SendToClient(client, message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        private void SendToClient(TcpClient tcpClient, Message message)
        {
            NetworkStream stream = tcpClient.GetStream();

            string jsonString = JsonSerializer.Serialize(message);

            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }
    }
}