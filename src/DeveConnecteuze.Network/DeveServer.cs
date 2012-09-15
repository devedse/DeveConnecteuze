using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace DeveConnecteuze.Network
{
    public class DeveServer
    {

        private int port;
        private TcpListener tcpListener;

        private List<DeveServerClient> clients = new List<DeveServerClient>();
        public List<DeveServerClient> Clients
        {
            get { return clients; }
        }

        private DeveQueue<DeveIncommingMessage> messages = new DeveQueue<DeveIncommingMessage>(100);

        private Boolean isRunning = false;
        public Boolean IsRunning
        {
            get { return isRunning; }
        }

        internal int maxMessageSize = 100000;
        public int MaxMessageSize
        {
            get { return maxMessageSize; }
            set { maxMessageSize = value; }
        }

        private Boolean shouldShutdown = false;

        public DeveServer(int port)
        {
            this.port = port;
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);

            tcpListener = new TcpListener(ip);
        }

        public void Start()
        {
            Thread tr = new Thread(new ThreadStart(Runner));
            //tr.IsBackground = true;
            tr.Start();
        }

        public void Stop()
        {
            shouldShutdown = true;
        }

        internal void AddDeveIncommingMessage(DeveIncommingMessage devInc)
        {
            messages.Enqueue(devInc);
        }

        public DeveIncommingMessage ReadMessage()
        {
            if (messages.Count == 0)
            {
                return null;
            }
            else
            {
                DeveIncommingMessage retval;
                Boolean didItWork = messages.TryDequeue(out retval);
                if (!didItWork)
                {
                    throw new Exception("Strange error");
                }
                return retval;
            }
        }

        private void Runner()
        {
            if (!isRunning)
            {
                isRunning = true;
                tcpListener.Start();

                while (!shouldShutdown)
                {
                    if (!tcpListener.Pending())
                    {
                        Thread.Sleep(100);
                    }
                    else
                    {
                        //blocks until a client has connected to the server
                        TcpClient client = tcpListener.AcceptTcpClient();
                        Console.WriteLine("Client connected");

                        DeveServerClient deveServerClient = new DeveServerClient(this, client);

                        lock (clients)
                        {
                            clients.Add(deveServerClient);
                        }

                        //Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                        //clientThread.Start(client);
                    }
                }

                foreach (DeveServerClient client in clients)
                {
                    client.Stop();
                }

                tcpListener.Stop();
                isRunning = false;
            }

        }


        /// <summary>
        /// Only for internal use
        /// </summary>
        /// <param name="client"></param>
        public void RemoveClient(DeveServerClient client)
        {
            lock (clients)
            {
                clients.Remove(client);
            }
        }

        public void SendToAll(DeveOutgoingMessage devOut)
        {
            lock (clients)
            {
                foreach (DeveServerClient deveServerClient in clients)
                {
                    deveServerClient.Send(devOut);
                }
            }
        }

    }
}
