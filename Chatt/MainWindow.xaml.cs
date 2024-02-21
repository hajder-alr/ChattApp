using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
        public MainWindow()
        {
            InitializeComponent();
            
            try 
            {
                client = new TcpClient("127.0.0.1", 1302);     

            } 
            catch (Exception ex) 
            { }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string messageToSend = ($"{usr.Text}|{psw.Text}");
            int byteCount = Encoding.ASCII.GetByteCount(messageToSend + 1);
            byte[] sendData = Encoding.ASCII.GetBytes(messageToSend);
            stream = client.GetStream();
            stream.Write(sendData, 0, sendData.Length);
            sr = new StreamReader(stream);
            string response = sr.ReadLine();
            msgbox.Text += response +"\n";
        }
    }
}
