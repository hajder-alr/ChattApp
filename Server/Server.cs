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
            string clientName = "NOT LOGGED IN";
            try
            {
                StringBuilder sb = new StringBuilder();
                byte[] buffer = new byte[1024]; // 1 KB buffer size
                int bytesRead;

                /*
                    Hade problem med att klienten skickade sammankopplade json's om man använder sendDataPacket flera gånger,
                    så vi skickar med ett "\n" från klienten efter varje request, så att vi kan seperera stringen och behandla json' datan enskilt
                */

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                    // Check if the received data contains the delimiter
                    string receivedData = sb.ToString();
                    int delimiterIndex;
                    while ((delimiterIndex = receivedData.IndexOf("\n")) != -1)
                    {
                        string jsonMessage = receivedData.Substring(0, delimiterIndex);

                        Request data = JsonSerializer.Deserialize<Request>(jsonMessage);

                        switch (data.Type)
                        {
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
                                        SendMessage(new Request() { Type = data.Type, Contents = new User() { Username = user.Username } }); //(fel här?)
                                                                                                                                             //loggedIn.Add(data.Sender);
                                        connectedClients.Add(user.Username, client);
                                        clientName = user.Username;
                                    }
                                    else
                                    {
                                        SendMessage(new Request() { Type = "error", Contents = user.Username });
                                        Console.WriteLine("Error: Not unique");
                                        //client.Close();
                                        break;
                                    }
                                }
                                else
                                {
                                    //OM INTE FINNS
                                    SendToClient(new Request() { Type = "error1" }, client);
                                    //client.Close();
                                    break;
                                }
                                break;
                            case "register":
                                RegisterUser(data, client);
                                break;
                            case "message":
                                if (!connectedClients.ContainsValue(client))
                                    break;

                                Message x = JsonSerializer.Deserialize<Message>((JsonElement)data.Contents);

                                if (x is Message m)
                                {
                                    SendMessage(new Request() { Type = "message", Contents = new Message() { Contents = x.Contents, Sender = x.Sender, Recipient = x.Recipient } });

                                    m.Status = "none";
                                    db.Messages.Add(m);
                                    db.SaveChanges();
                                }
                                break;
                            case "history":
                                if (!connectedClients.ContainsValue(client))
                                    break;

                                List<Message> messageList = db.Messages.ToList();

                                Request historyRequest = new Request() { Type = "history", Contents = messageList };

                                SendToClient(historyRequest, client);
                                break;
                            case "onlinelist":
                                if (!connectedClients.ContainsValue(client))
                                    break;

                                List<string> onlineUsers = connectedClients.Keys.ToList();

                                SendToClient(new Request() { Type = "onlinelist", Contents = onlineUsers }, client);
                                break;
                            default:
                                break;
                        }

                        //Console.WriteLine($"[{clientName}] -> [Server] :: {jsonMessage}");
                        ConsoleColorClient(clientName, "[Server]", jsonMessage);
                        receivedData = receivedData.Substring(delimiterIndex + 1);
                    }

                    sb.Clear();
                    sb.Append(receivedData);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("IOException: " + ex.Message);
            }
            finally
            {
                stream.Close();
                client.Close();
            }
        }

        private void SendMessage(Request message)
        {
            foreach (var tcp in connectedClients)
            {
                TcpClient client = tcp.Value;
                NetworkStream stream = client.GetStream();

                string jsonString = JsonSerializer.Serialize(message);
                ConsoleColorServer("[Server] -> ", tcp.Key, jsonString);

                byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
        }

        private void SendToClient(Request message, TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            string jsonString = JsonSerializer.Serialize(message);

            string foundKey = connectedClients.FirstOrDefault(x => x.Value == client).Key;

            //Console.WriteLine($"[Server] -> {foundKey} :: {jsonString}");
            ConsoleColorServer("[Server]", foundKey, jsonString);

            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
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

                SendToClient(message, client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        //private void SendToClient(TcpClient tcpClient, Request message)
        //{
        //    NetworkStream stream = tcpClient.GetStream();
        //
        //    string jsonString = JsonSerializer.Serialize(message);
        //
        //    byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
        //
        //    stream.Write(buffer, 0, buffer.Length);
        //    stream.Flush();
        //}
        private void ConsoleColorServer(string serverText, string keyText, string jsonString)
        {
            // Set colors for each part of the string
            Console.ForegroundColor = ConsoleColor.DarkRed; // Server text in red
            Console.Write(serverText);

            Console.ForegroundColor = ConsoleColor.DarkYellow; // Orange color is not available in ConsoleColor, using DarkYellow as an alternative
            Console.Write(" -> ");

            Console.ForegroundColor = ConsoleColor.DarkRed; // Orange color is not available in ConsoleColor, using DarkYellow as an alternative
            Console.Write(keyText);

            Console.ForegroundColor = ConsoleColor.DarkGray; // Separator in red
            Console.Write(" :: ");

            Console.ForegroundColor = ConsoleColor.DarkRed; // JSON string in white
            Console.WriteLine(jsonString);

            // Reset colors to default
            Console.ResetColor();
        }

        private void ConsoleColorClient(string serverText, string keyText, string jsonString)
        {
            // Set colors for each part of the string
            Console.ForegroundColor = ConsoleColor.DarkYellow; // Server text in red
            Console.Write(serverText);

            Console.ForegroundColor = ConsoleColor.DarkRed; // Orange color is not available in ConsoleColor, using DarkYellow as an alternative
            Console.Write(" -> ");

            Console.ForegroundColor = ConsoleColor.DarkYellow; // Orange color is not available in ConsoleColor, using DarkYellow as an alternative
            Console.Write(keyText);

            Console.ForegroundColor = ConsoleColor.DarkGray; // Separator in red
            Console.Write(" :: ");

            Console.ForegroundColor = ConsoleColor.DarkYellow; // JSON string in white
            Console.WriteLine(jsonString);

            // Reset colors to default
            Console.ResetColor();
        }
    }
}