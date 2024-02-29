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
using System.Diagnostics;
using System.Data;

namespace Chatt
{
    public partial class MainWindow : Window
    {
        TcpClient client;
        NetworkStream stream;
        Message message;
        public string PasswordInput { get; set; }

        public string Usernametemp { get; set; }

        //Request request;
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
                    if (Application.Current.MainWindow.WindowState != WindowState.Maximized)
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
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        private void Button_Click(object s, RoutedEventArgs e)
        {
            try
            {
                Request request = new Request();

                request.Type = "message";

                request.Contents = new Message() { Contents = msg.Text, Sender = username.Text, Recipient = "chatroom" };

                sendDataPacket(request);
            }
            catch
            {
                UpdateTextBox($"Logga in", msgbox); //Lägger alltid till en ny rad text i textloggen, om man skriver utan att vara inloggad
            }
        }

        private void UpdateTextBox(string text, TextBlock textblock)
        {
            if (!textblock.Dispatcher.CheckAccess())
            {
                textblock.Dispatcher.Invoke(() => UpdateTextBox(text, textblock));
                return;
            }
            textblock.Text += text + "\n";
        }

        private void ReceivingTask(NetworkStream stream, TcpClient client)
        {
            try
            {
                sendDataPacket(new Request() { Type = "history", Contents = "null" });
                sendDataPacket(new Request() { Type = "onlinelist", Contents = "null" });
                bool quit = false;
                while (!quit)
                {
                    byte[] receiveData = new byte[1024];
                    int bytesReceived = stream.Read(receiveData, 0, receiveData.Length);
                    string response = Encoding.UTF8.GetString(receiveData, 0, bytesReceived);

                    Request data = JsonSerializer.Deserialize<Request>(response)!;

                    switch (data.Type)
                    {
                        case "login":
                            {
                                User user = JsonSerializer.Deserialize<User>((JsonElement)data.Contents);
                                UpdateTextBox($"[{user.Username}]", ConnectedUserBox);
                                break;
                            }
                        case "register":
                            {
                                if (data.Contents.ToString() == "successful") // Servern får skicka tillbaka samma användarnamn & lösenord om kontot är skapat. Annars skickas inget
                                {
                                    UpdateTextBox("Account has been created", msgbox);
                                    // sendDataPacket(new Request() { Type = "login", Contents = new User() { Username = username.Text, Password = PasswordInput } });
                                }

                                else UpdateTextBox("\nUser name already taken", msgbox);
                                break;
                            }
                        case "message":
                            //UpdateTextBox(data.Contents.GetType().ToString(), msgbox);
                            Message xf = JsonSerializer.Deserialize<Message>((JsonElement)data.Contents);

                            UpdateTextBox($"[{xf.Sender}]: {xf.Contents}", msgbox);
                            break;
                        case "error":
                            /*
							              MessageBox.Show(ex.Message, "Message", MessageBoxButton.OK, MessageBoxImage.Error);
							              UpdateTextBox($"[{data.Sender}]: fel", msgbox);
							              MessageBox.Show("Login Error",$"[{data.Sender}]: fel", MessageBoxButton.OK, MessageBoxImage.Error);
                            */
                            MessageBox.Show("Redan inloggad", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            //  ^Skickar detta till alla clienter, men ska bara till den som gör fel - Fixat
                            break;
                        case "error1":
                            MessageBox.Show("Användare finns ej eller fel lössenord", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        case "history":
                            List<Message> history = JsonSerializer.Deserialize<List<Message>>((JsonElement)data.Contents);

                            foreach (Message m in history)
                            {
                                UpdateTextBox($"[{m.Sender}]: {m.Contents}", msgbox);
                            }
                            break;
                        case "onlinelist":
                            List<string> users = JsonSerializer.Deserialize<List<string>>((JsonElement)data.Contents);
                            foreach (string user in users)
                            {
                                UpdateTextBox($"[{user}]", ConnectedUserBox);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (ThreadAbortException) { Console.WriteLine("Receiving thread aborted."); }
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
                Request request = new Request();

                request.Type = command;
                request.Contents = new User() { Password = PasswordInput, Username = username.Text };

                if (command == "register")
                    sendDataPacket(request);

                sendDataPacket(new Request() { Type = "login", Contents = new User() { Password = PasswordInput, Username = username.Text } });

                Thread receivingThread = new Thread(() => ReceivingTask(stream, client));
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

        private void sendDataPacket(Request message)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(message);
                string messageWithDelimiter = jsonString + "\n"; // Add a newline delimiter
                byte[] sendData = Encoding.UTF8.GetBytes(messageWithDelimiter);
                stream.Write(sendData, 0, sendData.Length);
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error sending data: " + ex.Message);
            }
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
