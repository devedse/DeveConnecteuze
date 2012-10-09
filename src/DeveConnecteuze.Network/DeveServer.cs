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
    public class DeveServer : DevePeer
    {
        private Boolean shouldShutdown = false;
        private int port;
        private TcpListener tcpListener;

        private List<DeveConnection> clientConnections = new List<DeveConnection>();
        public List<DeveConnection> Clients
        {
            get { return clientConnections; }
        }

        public DeveServer(int port)
        {
            this.port = port;
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);

            tcpListener = new TcpListener(ip);
        }

        public override void Start()
        {
            Thread tr = new Thread(new ThreadStart(Runner));
            //tr.IsBackground = true;
            tr.Start();
        }

        public override void Stop()
        {
            shouldShutdown = true;
        }

        private void Runner()
        {
         
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

                        DeveConnection deveClientConnection = new DeveConnection(client, this);
                        deveClientConnection.Start();

                        lock (clientConnections)
                        {
                            clientConnections.Add(deveClientConnection);
                        }

                        //Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                        //clientThread.Start(client);
                    }
                }

                foreach (DeveConnection deveClientConnection in clientConnections)
                {
                    deveClientConnection.Stop();
                }

                tcpListener.Stop();

            

        }


        /// <summary>
        /// Only for internal use
        /// </summary>
        /// <param name="client"></param>
        internal void RemoveClient(DeveConnection client)
        {
            lock (clientConnections)
            {
                clientConnections.Remove(client);
            }
        }

        public void SendToAll(DeveOutgoingMessage devOut)
        {
            lock (clientConnections)
            {
                foreach (DeveConnection deveClientConnection in clientConnections)
                {
                    deveClientConnection.Send(devOut);
                }
            }
        }

    }
}
