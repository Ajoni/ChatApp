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
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        HashSet<string> ClientNames = new HashSet<string>();
        private static string endOfMessage = "/^^^/";
        public MainWindow()
        {
            InitializeComponent();
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
            if(ServPort.Text == String.Empty)
            {
                MessageBox.Show("Specify port");
                return;
            }
            serverSocket.Bind(new IPEndPoint(GetLocalIP(), Convert.ToInt32(ServPort.Text)));
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
            switch (text[0])
            {
            //add to connected list and realay to other clients
                case "/!hi":
                    text[1] = text[1].Replace(endOfMessage, String.Empty);
                    if (ClientNames.Contains(text[1]))
                    {
                        string clientIP = $"({((System.Net.IPEndPoint)current.RemoteEndPoint).Address})";
                        text[1] += clientIP;
                        current.Send(Encoding.ASCII.GetBytes($"/!er {clientIP}{endOfMessage}"));//inform client about its name being a dupeplicate of other client (client localy adds ip to its name)
                    }
                RelayHiToClients(current, Encoding.ASCII.GetBytes($"{text[0]} {text[1]}{endOfMessage}")); 
                    InvokeConnectedAdd(text[1]);
                    break;

             //remove from connected list and realay to other clients
                case "/!exit":
                current.Shutdown(SocketShutdown.Both);
                RelayToClients(current, recBuf);
                ClientNames.Remove(text[1].Replace(endOfMessage, String.Empty));
                current.Close();
                clientSockets.Remove(current);
                    InvokeConnectedRemove(text[1]);
                return;      
                    
             //relay current client's msg to other clients
                default:            
                RelayToClients(current, recBuf);
                    break;
            } 

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }

        private void RelayToClients(Socket sender, byte[] text)
        {
            foreach (Socket sck in clientSockets)
            {
                if (sck != sender)
                {
                    sck.Send(text);
                }
            }
        }

        private void RelayHiToClients(Socket sender, byte[] NewClientName)
        {
            foreach (Socket sck in clientSockets)
            {
                if (sck != sender) //send new clients names to 'older' clients
                {
                    sck.Send(NewClientName);
                }
                else               //send 'older' clients names to new client
                {
                    foreach (string s in ClientNames)
                    {
                        string text = $"/!hi {s}{endOfMessage}";
                        sck.Send(Encoding.ASCII.GetBytes(text));
                    }
                }
            }
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            SetupServer();
            StartBtn.IsEnabled = false;
            StartBtn.Content = "Started";
        }

        private void InvokeConnectedAdd(string text)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectedList.Items.Add(text);
                ClientNames.Add(text);
            });
        }

        private void InvokeConnectedRemove(string text)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectedList.Items.Remove(text);
                ClientNames.Remove(text);
            });
        }
    }
}
