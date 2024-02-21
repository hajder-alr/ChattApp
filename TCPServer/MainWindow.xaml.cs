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

namespace TCPServer
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

		SimpleTcpServer server;
		
		private void btnStart_Click(object sender, EventArgs e)
		{
			try
			{
				server.Start();
				txtInfo.Text+=$"Starting...{Environment.NewLine}";
				btnStart.IsEnabled = false;
				btnSend.IsEnabled = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An error occurred during server startup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void MainWindow_Loaded(object sender, EventArgs e)
		{
			btnSend.IsEnabled = false;
			server = new SimpleTcpServer("127.0.0.1:9000");
			server.Events.ClientConnected +=Events_ClientConnected;
			server.Events.ClientDisconnected +=Events_ClientDisconnected;
			server.Events.DataReceived +=Events_DataReceived;
		}

		private void Events_DataReceived(object? sender, DataReceivedEventArgs e)
		{
			txtInfo.Text += $"{e.IpPort}: {Encoding.UTF8.GetString(e.Data)}{Environment.NewLine}";
		}

		private void Events_ClientDisconnected(object? sender, ConnectionEventArgs e)
		{
			txtInfo.Text += $"{e.IpPort}: disconnected.{Environment.NewLine}";
			lstClientIP.Items.Remove(e.IpPort);
		}

		private void Events_ClientConnected(object? sender, ConnectionEventArgs e)
		{
			txtInfo.Text += $"{e.IpPort}: connected.{Environment.NewLine}";
			lstClientIP.Items.Add(e.IpPort);
		}

		private void btnSend_Click(object sender, RoutedEventArgs e)
		{
			if(server.IsListening)
			{
				if (!string.IsNullOrEmpty(txtMessage.Text) && lstClientIP.SelectedItems != null)
				{
					server.Send(lstClientIP.SelectedItems.ToString(), txtMessage.Text);
					txtInfo.Text += $"Server: {txtMessage.Text}{Environment.NewLine}";
					txtMessage.Text = string.Empty;
				}
			}
		}
	}
}
