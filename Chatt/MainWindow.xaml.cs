using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
namespace Chatt
{
    public partial class MainWindow : Window
    {
        TcpClient client;
        NetworkStream stream;
        Message message;
        public MainWindow()
        {
            InitializeComponent();
        }
     
        private void Button_Click(object sender, RoutedEventArgs e)
        {    
            message.MessageContents = msg.Text;
            message.Type = "message";
            sendDataPacket(message);
        }
        private  void UpdateTextBox(string text)
        {
            if (!msgbox.Dispatcher.CheckAccess())
            {
                msgbox.Dispatcher.Invoke(() => UpdateTextBox(text));
                return;
            }
            msgbox.Text += text + "\n";
        }

        private void ReceivingTask(NetworkStream stream,TcpClient client)
        {
            try
            {
                while (true)
                {
                    byte[] receiveData = new byte[1024];
                    int bytesReceived = stream.Read(receiveData, 0, receiveData.Length);
                    string response = Encoding.UTF8.GetString(receiveData, 0, bytesReceived);
                    Message data = JsonSerializer.Deserialize<Message>(response)!;
                    UpdateTextBox($"[{data.Sender}]: {data.MessageContents}");
                }
            }
            catch (ThreadAbortException){ Console.WriteLine("Receiving thread aborted."); }
            finally { stream.Close(); client.Close(); }
        }
        private void Startup() 
        {
            message = new Message();
            connection:
            try
            {
                client = new TcpClient("127.0.0.1", 1302);
                stream = client.GetStream();
                message.Sender = sender.Text;
                message.Username = message.Sender;
                message.Password = password.Text;
                message.Type = "login";
                message.MessageContents = "has connected";
                sendDataPacket(message);
                Thread receivingThread = new Thread(() => ReceivingTask(stream,client));
                receivingThread.Start();
            }
            catch (Exception)
            {
                Console.WriteLine("failed to connect...");
                goto connection;
            }

        }

        private void btnSender1_Click(object sender, RoutedEventArgs e)
        {
            Startup();
        }

        private void sendDataPacket(Message message) 
        {
            string jsonString = JsonSerializer.Serialize(message);
            int byteCount = Encoding.ASCII.GetByteCount(jsonString + 1);
            byte[] sendData = Encoding.ASCII.GetBytes(jsonString);
            stream.Write(sendData, 0, sendData.Length);
        }
    }

}
