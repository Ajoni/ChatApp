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
using System.Net;
using System.Net.Sockets;

namespace ChatApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 60000;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        public MainWindow()
        {
            InitializeComponent();
            ServPort.Text = PORT.ToString();
            ServIP.Text = GetLocalIP().ToString();
        }

        private static IPAddress GetLocalIP()
        {
            try
            {
                if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    throw new Exception("No local IPv4 interface");
                }
            }
            catch (Exception e)
            {

                MessageBox.Show(e.ToString());
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        private  void SetupServer()
        {
            serverSocket.Bind(new IPEndPoint(GetLocalIP(), PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }

        private  void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            //Client connected, waiting for request...
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private  void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string[] text = Encoding.ASCII.GetString(recBuf).Split(' ');

            if (text[0].ToLower() == "/!hi") // Add client to connected list
            {
                ConnectedList.Items.Add(text[1]);   //throws threading excpetion
            }
            else
            if (text[0].ToLower() == "/!exit") // Client wants to exit gracefully
            {
                // Always Shutdown before closing
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                clientSockets.Remove(current);
                return;
            }
            else    //send current client's msg to other clients
            {
                foreach (Socket sck in clientSockets)
                {
                    if (sck != current)
                    {
                        sck.Send(recBuf);
                     }
            }
            }

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            SetupServer();
            StartBtn.IsEnabled = false;
            StartBtn.Content = "Started";
        }
    }
}
