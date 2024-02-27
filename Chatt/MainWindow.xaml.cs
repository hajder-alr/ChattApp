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
using System.Windows.Interop;
namespace Chatt
{
    public partial class MainWindow : Window
    {
        TcpClient client;
        NetworkStream stream;
        Message message;
		public string PasswordInput { get; set; }
		public MainWindow()
        {
            InitializeComponent();
        }
        private void BorderClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            switch (int.Parse(btn.Uid)) 
            { 
                case 1:
                    Application.Current.MainWindow.WindowState = WindowState.Minimized;
                    break;
                case 2:
                    if(Application.Current.MainWindow.WindowState != WindowState.Maximized) 
                    { Application.Current.MainWindow.WindowState = WindowState.Maximized; }
                    else { Application.Current.MainWindow.WindowState = WindowState.Normal; }
                    break;
                case 3:
                    Application.Current.Shutdown();
                    break;   

            }
            
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed) 
            {
                DragMove();
            }
        }
         private void Button_Click(object sender, RoutedEventArgs e)
         {    
             message.MessageContents = msg.Text;
             message.Type = "message";
             sendDataPacket(message);
         }
         private  void UpdateTextBox(string text,TextBlock textblock)
         {
             if (!textblock.Dispatcher.CheckAccess())
             {
                textblock.Dispatcher.Invoke(() => UpdateTextBox(text,textblock));
                return;
             }
             textblock.Text += text + "\n";
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
                    switch (data.Type)
                    {
                        case "login":
                            UpdateTextBox($"[{data.Sender}]", ConnectedUserBox);
                            break;
                        case "register":
                            if (data.MessageContents == "successful") UpdateTextBox("Account has been created", msgbox);
                            else UpdateTextBox("User name already taken", msgbox);
                            break;
                        case "message":
                            UpdateTextBox($"[{data.Sender}]: {data.MessageContents}", msgbox);
                            break;
                        default:
                            break;
                    }
                    
                 }
             }
             catch (ThreadAbortException){ Console.WriteLine("Receiving thread aborted."); }
             finally { stream.Close(); client.Close(); }
         }
         private void Startup(string command) 
         {
             message = new Message();
             connection:
             try
             {
                client = new TcpClient("127.0.0.1", 1302);
                stream = client.GetStream();
                message.Sender = username.Text;
                message.Username = username.Text;
                message.Password = PasswordInput;
                //message.Password = password.Text;
                message.Type = command;
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

        private void Login(object sender, RoutedEventArgs e)
        {
            Startup("login");
        }

		private void Register(object sender, RoutedEventArgs e)
		{
			if (username.Text.Length < 4 || username.Text.Length > 16 || username.Text == "Username")
			{
				msgbox.Text = "Username needs to be between 4 and 16 characters";
			}
			else if (username.Text.Any(char.IsWhiteSpace))
			{
				msgbox.Text = "Username cannot contain white space";
			}
			else if (PasswordInput.Length < 4 || PasswordInput.Length > 16 || PasswordInput == "password")
			{
				msgbox.Text = "Password needs to be between 4 and 16 characters";
			}
			else if (PasswordInput.Any(char.IsWhiteSpace))
			{
				msgbox.Text = "Password cannot contain white space";
			}
			else
			{
				Startup("register");
			}
		}

		private void sendDataPacket(Message message) 
         {
             string jsonString = JsonSerializer.Serialize(message);
             int byteCount = Encoding.ASCII.GetByteCount(jsonString + 1);
             byte[] sendData = Encoding.ASCII.GetBytes(jsonString);
             stream.Write(sendData, 0, sendData.Length);
         }

		private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			PasswordInput = passwordBox.Password;
		}
		private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (passwordBox.Password == "password")
			{
				passwordBox.Password = "";
			}
		}
		private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(passwordBox.Password))
			{
				passwordBox.Password = "password";
			}
		}
		private void PasswordBox_Loaded(object sender, RoutedEventArgs e)
		{
			// Check the initial focus state when the program starts
            PasswordBox_LostFocus(sender, e);

		}
		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (username.Text == "Username")
			{
				username.Text = "";
			}
		}
		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(username.Text))
			{
				username.Text = "Username";
			}
		}
		private void TextBox_Loaded(object sender, RoutedEventArgs e)
		{
			// Check the initial focus state when the program starts
			TextBox_LostFocus(sender, e);
		}
	}

}
