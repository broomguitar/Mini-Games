using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.Windows.Documents;
using System.CodeDom;

namespace mini_games
{
    public class SocketServer
    {
        private static Socket _server;
        private static bool isRunning = false;
        public static event EventHandler<Client> HasNewClientEvent;
        public static event EventHandler<MessageData> HasNewMsgEvent;
        public static event EventHandler ClientOfflineEvent;
        private static readonly List<Client> tcpClients = new List<Client>();
        private static Queue<Client> removeQueue = new Queue<Client>();
        private static Task _monitorTask;
        public static void Open(IPAddress iPAddress, int port)
        {
            try
            {
                _server = new Socket(iPAddress.AddressFamily,SocketType.Stream,ProtocolType.Tcp);
                _server.Bind(new IPEndPoint(iPAddress, port));
                _server.Listen(1024);
                _server.BeginAccept(new AsyncCallback(HasNewClient), _server);
                if (!isRunning)
                {
                    if (_server != null)
                    {
                        isRunning = true;
                        if (_monitorTask == null || _monitorTask.IsCompleted)
                        {
                            _monitorTask = Task.Factory.StartNew(
                                () =>
                                {
                                    while (isRunning)
                                    {
                                        checkClientsStatus();
                                        System.Threading.Thread.Sleep(2000);
                                    }
                                }, TaskCreationOptions.LongRunning);
                        }
                    }
                    else
                    {
                        isRunning = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _server = null;
                Console.WriteLine(ex.ToString());
            }
        }
        public static void Close()
        {
            if (_server != null)
            {
                try
                {
                    _server.Close();
                    isRunning = false;
                    foreach (var item in tcpClients)
                    {
                        item.Close();
                    }
                    tcpClients.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
        private static void HasNewClient(IAsyncResult asyncResult)
        {
            if(!isRunning)
            {
                return;
            }
            Socket server = (Socket)asyncResult.AsyncState;
            try
            {
                Socket tcpClient = server.EndAccept(asyncResult);
                tcpClient.IOControl(IOControlCode.KeepAliveValues, GetKeepAliveData(), null);
                tcpClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                Client client = new Client(tcpClient);
                tcpClients.Add(client);
                client.RecvDataBuffer= new byte[1];
                HasNewClientEvent?.Invoke(server, client);
                client.NetworkStream.BeginRead(client.RecvDataBuffer, 0, client.RecvDataBuffer.Length, HasNewMsg,client);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                server.BeginAccept(new AsyncCallback(HasNewClient), server);
            }
        }
        private static void HasNewMsg(IAsyncResult asyncResult)
        {
            if (isRunning)
            {
                Client client = (Client)asyncResult.AsyncState;
                if(!client.ClientSocket.Connected)
                {
                    return;
                }
                try
                {
                    int recv = client.NetworkStream.EndRead(asyncResult);
                    if (recv > 0)
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        string message = Encoding.GetEncoding("gb2312").GetString(client.RecvDataBuffer, 0, recv);
                        HasNewMsgEvent?.Invoke(client,new MessageData(client,message));
                    }
                    else
                    {
                        Debug.WriteLine($"客户端【{((IPEndPoint)client.ClientSocket.RemoteEndPoint).Address}:{((IPEndPoint)client.ClientSocket.RemoteEndPoint).Port}】断开", false);
                    }
                }
                catch (SocketException se)
                {
                    Debug.WriteLine(se.ToString());
                }
                finally
                {
                    if (tcpClients.Contains(client))
                    //继续接收来自来客户端的数据
                    {
                        client.NetworkStream.BeginRead(client.RecvDataBuffer, 0, client.RecvDataBuffer.Length,
                     new AsyncCallback(HasNewMsg), client);
                    }
                }
            }
        }
        public static bool SendData(string msg)
        {
            try
            {
                if (tcpClients.Count < 1) return false;
                foreach (var item in tcpClients)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    item.ClientSocket.Send(buffer);
                    //var bw = new System.IO.BinaryWriter(item.GetStream());
                    //bw.Write(msg);
                }
                Console.WriteLine($"发送给客户端指令【{msg}】");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        private static void checkClientsStatus()
        {
            try
            {
                lock (tcpClients)
                {
                    foreach (var item in tcpClients)
                    {
                        //if (item.Client.Client.Poll(500, System.Net.Sockets.SelectMode.SelectRead) && (item.Client.Client.Available == 0))
                        if (!isClientConnected(item))
                        {
                            removeQueue.Enqueue(item);
                            ClientOfflineEvent?.Invoke(item, null);
                            continue;
                        }
                    }
                    while (removeQueue.Count > 0)
                    {
                        Client item = removeQueue.Dequeue();
                        tcpClients.Remove(item);
                        try
                        {
                            item.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("关闭客户端连接" + ex.ToString());
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static bool isClientConnected(Client client)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation c in tcpConnections)
            {
                TcpState stateOfConnection = c.State;

                if (c.LocalEndPoint.Equals(client.ClientSocket.LocalEndPoint) && c.RemoteEndPoint.Equals(client.ClientSocket.RemoteEndPoint))
                {
                    if (stateOfConnection == TcpState.Established)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }

            }

            return false;
        }

        private static byte[] GetKeepAliveData()
        {
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)3000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
            return inOptionValues;
        }
    }
    public class Client
    {
        public byte[] RecvDataBuffer
        {
            get;set;
        }
        public IPAddress IP
        {
            get
            {
                if (_clientSocket != null)
                {
                    return ((IPEndPoint)_clientSocket.RemoteEndPoint).Address;
                }
                return null;
            }
        }
        public int Port
        {
            get
            {
                if (_clientSocket != null)
                {
                    return ((IPEndPoint)_clientSocket.RemoteEndPoint).Port;
                }
                return -1;
            }
        }
        public string Datagram
        {
            get;set;
        }
        private Socket _clientSocket;
        private NetworkStream  _networkStream;
        /// <summary>
        /// 获得与客户端会话关联的Socket对象
        /// </summary>
        public Socket ClientSocket
        {
            get
            {
                return _clientSocket;

            }
        }
        public Client(Socket client)
        {
            _clientSocket = client;
            _networkStream = new NetworkStream(client,true);
        }
        public NetworkStream NetworkStream
        {
            get
            {
                return _networkStream;
            }
        }
        public void Close()
        {
            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.Close();
        }
        
    }
    public class MessageData
    {
        public MessageData(Client client,string msg)
        {
            RemoteClient = client;
            Message = msg;
        }
        public Client RemoteClient { get; set; }
        public string Message { get; set; }
    }
}
