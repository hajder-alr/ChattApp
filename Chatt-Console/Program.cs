using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Message message = new Message();
        connection:
            try
            {
                TcpClient client = new TcpClient("127.0.0.1", 1302);

                NetworkStream stream = client.GetStream();

                Console.WriteLine("Enter your name: ");
                message.Sender = Console.ReadLine();
                message.Username = message.Sender;
                message.Password = "hello";
                message.Type = "getallusers";
                //message.Type = "register";


                Console.WriteLine("Enter recipient name: ");
                message.Recipient = Console.ReadLine();

                Task receivingTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        byte[] receiveData = new byte[1024];
                        int bytesReceived = await stream.ReadAsync(receiveData, 0, receiveData.Length);

                        string response = Encoding.UTF8.GetString(receiveData, 0, bytesReceived);

                        Message data = JsonSerializer.Deserialize<Message>(response)!;

                        Console.WriteLine($"\nSERVER RESPONSE:{data.Sender}: {data.MessageContents}");
                    }
                });

                while (true)
                {
                    Console.Write("> ");
                    message.MessageContents = Console.ReadLine();

                    if (string.IsNullOrEmpty(message.MessageContents))
                        break;

                    string jsonString = JsonSerializer.Serialize(message);

                    int byteCount = Encoding.ASCII.GetByteCount(jsonString + 1);
                    byte[] sendData = Encoding.ASCII.GetBytes(jsonString);
                    stream.Write(sendData, 0, sendData.Length);
                }
                await receivingTask;
                stream.Close();
                client.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("failed to connect...");
                goto connection;
            }
        }
    }
}