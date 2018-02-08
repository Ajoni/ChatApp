using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private static readonly Socket ClientSocket = new Socket
       (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Thread op;

        //private const int PORT = 100;
        public MainWindow()
        {
            InitializeComponent();
        }

        private static void ConnectToServer(string ip, int port)
        {
            //int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    //attempts++;
                    //Console.WriteLine("Connection attempt " + attempts);
                    // Change IPAddress.Loopback to a remote IP to connect to a remote host.
                    ClientSocket.Connect(IPAddress.Parse(ip), port);
                    
                    
                }
                catch (SocketException)
                {
                    
                }
            }

        }

        /// <summary>
        /// Close socket and exit program.
        /// </summary>
        private void Exit()
        {
            SendString($"/!exit {NameBox.Text}"); // Tell the server we are exiting
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// Sends a string to the server with ASCII encoding.
        /// </summary>
        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private void ReceiveResponse()
        {
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            ChatBox.Items.Add(text);
        }
        private void InvokeReceive()
        {
            op = new Thread(new ThreadStart((Action)(() =>
            {
                while (true)
                {
                    ReceiveResponse();
                }
            })));
            op.Name = "RecevieThread";
            op.Start();

        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            ConnectToServer(IPBox.Text,Convert.ToInt32(PortBox.Text));
            SendString($"/!hi {NameBox.Text}");
            ConnectBtn.IsEnabled = false;
            DcBtn.IsEnabled = true;
            SendBtn.IsEnabled = true;
            MsgBox.Text = "";
            MsgBox.Opacity = 1.0;
            Task.Factory.StartNew(() =>
            {
                InvokeReceive();
            });
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            string msg = $"{NameBox.Text}: {MsgBox.Text}";
            ChatBox.Items.Add(msg);
            SendString(msg);
        }

        private void DcBtn_Click(object sender, RoutedEventArgs e)
        {
            op.Abort();
            Exit();
        }
    }
}
