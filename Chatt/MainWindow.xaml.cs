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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient client;
        NetworkStream stream;
        StreamReader sr;
        Message message;
        BackgroundWorker backgroundWorker1;
        public MainWindow()
        {
            InitializeComponent();
            
           
        }

        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           
                message.MessageContents = msg.Text;
                string jsonString = JsonSerializer.Serialize(message);
                int byteCount = Encoding.ASCII.GetByteCount(jsonString + 1);
                byte[] sendData = Encoding.ASCII.GetBytes(jsonString);
                stream.Write(sendData, 0, sendData.Length);
                checkformessages();

        }
        private void checkformessages() 
        {
            byte[] buffer = new byte[1024];
            int recv = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.UTF8.GetString(buffer, 0, recv);
      
            Message data = JsonSerializer.Deserialize<Message>(request);

            if (data != null) 
            {                
                 msgbox.Text += ($"[{data.Sender}]: {data.MessageContents} \n");
            }
                
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
                         message.Type = "message";

                         Console.WriteLine("Enter recipient name: ");
                         message.Recipient = "1";
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
    }

}
