using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public MainWindow()
        {
            InitializeComponent();
        }

        SimpleTcpClient client;

		private void btnSend_Click(object sender, RoutedEventArgs e)
		{
			if(client.IsConnected)
			{
				if(!string.IsNullOrEmpty(txtMessage.Text))
				{
					client.Send(txtMessage.Text);
					txtInfo.Text =$"Me: {txtMessage.Text}{Environment.NewLine}";
					txtMessage.Text = string.Empty;
				}
			}
			else
			{
				txtInfo.Text +="Message";
			}
		}

		private void btnConnect_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				client.Connect();
				btnSend.IsEnabled = true;
				btnConnect.IsEnabled = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Message",MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}
        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            client = new(txtIP.Text);
			client.Events.Connected +=Events_Connected;
			client.Events.DataReceived +=Events_DataReceived;
			client.Events.Disconnected +=Events_Disconnected;
			btnSend.IsEnabled = false;
        }

		private void Events_Disconnected(object? sender, ConnectionEventArgs e)
		{
			txtInfo.Text += $"Server disconnected.{Environment.NewLine}";
		}

		private void Events_DataReceived(object? sender, DataReceivedEventArgs e)
		{
			txtInfo.Text += $"Server: {Encoding.UTF8.GetString(e.Data)}{Environment.NewLine}";
		}

		private void Events_Connected(object? sender, ConnectionEventArgs e)
		{
			txtInfo.Text += $"Server connected.{Environment.NewLine}";
		}
	}
}
