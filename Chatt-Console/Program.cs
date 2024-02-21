using System.Net.Sockets;
using System.Text;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
        connection:
            try
            {
                string messageToSend;
                TcpClient client = new TcpClient("127.0.0.1", 1302);
                NetworkStream stream = client.GetStream();

                while (true)
                {
                    messageToSend = Console.ReadLine();
                    if (string.IsNullOrEmpty(messageToSend))
                        break;

                    int byteCount = Encoding.ASCII.GetByteCount(messageToSend + 1);
                    byte[] sendData = Encoding.ASCII.GetBytes(messageToSend);
                    stream.Write(sendData, 0, sendData.Length);
                }
                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("failed to connect...");
                goto connection;
            }
        }
    }
}