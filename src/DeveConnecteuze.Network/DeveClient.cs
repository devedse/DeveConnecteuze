using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace DeveConnecteuze.Network
{
    public class DeveClient
    {
        private TcpClient tcpClient;

        private Boolean shouldShutdown = false;

        private DeveQueue<DeveIncommingMessage> messages = new DeveQueue<DeveIncommingMessage>(100);
        private DeveQueue<DeveOutgoingMessage> messagesToSendQueue = new DeveQueue<DeveOutgoingMessage>(10);

        private Boolean isRunning = false;
        public Boolean IsRunning
        {
            get { return isRunning; }
        }

        private DateTime lastKeepAlive = DateTime.Now;
        private TimeSpan keepAliveTimer = new TimeSpan(0, 0, 5);

        private int maxMessageSize = 100000;
        public int MaxMessageSize
        {
            get { return maxMessageSize; }
            set { maxMessageSize = value; }
        }

        private int size = 4096;
        private byte[] receiveBuffer;
        private NetworkStream networkStream;

        private String iporhost;
        private int port;

        public DeveClient(String iporhost, int port)
        {
            receiveBuffer = new byte[size];
            this.iporhost = iporhost;
            this.port = port;
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

        public void Start()
        {
            Thread tr = new Thread(new ThreadStart(Runner));
            //tr.IsBackground = true;
            tr.Start();
        }

        public void Stop()
        {
            shouldShutdown = true;
            tcpClient.Close();
        }


        private void Sender()
        {
            while (!shouldShutdown && tcpClient.Connected)
            {
                DeveOutgoingMessage devOut;
                Boolean worked = messagesToSendQueue.TryDequeue(out devOut);

                if (worked)
                {
                    try
                    {
                        NetworkStream networkStream = tcpClient.GetStream();
                        Byte[] b = devOut.GetBytes();
                        networkStream.Write(b, 0, b.Length);
                        networkStream.Flush();

                        lastKeepAlive = DateTime.Now;
                    }
                    catch
                    {
                        Console.WriteLine("Client probably disconnected, message not send");
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }

                CheckAndSendKeepAliveIfNeeded();
            }
        }


        public void Runner()
        {
            if (!isRunning)
            {
                isRunning = true;

                tcpClient = new TcpClient();

                tcpClient.Connect(iporhost, port);
                this.AddDeveIncommingMessage(new DeveIncommingMessage(null, new byte[2] { (byte)DeveMessageType.StatusChanged, (byte)NetworkStatus.Connected }));

                networkStream = tcpClient.GetStream();

                tcpClient.SendBufferSize = size;
                tcpClient.ReceiveBufferSize = size;

                Thread tr = new Thread(Sender);
                tr.Start();

                while (!shouldShutdown && tcpClient.Connected)
                {
                    try
                    {
                        byte[] firstLengthIntByteArray = ReadThisAmmountOfBytes(4);
                        int bytesToRead = BitConverter.ToInt32(firstLengthIntByteArray, 0);

                        if (bytesToRead > this.maxMessageSize)
                        {
                            Console.WriteLine("Warning: I'm gonna receive a big message of the size: " + bytesToRead);
                            //throw new InvalidDataException("This message is probably a bit big :), the size is: " + bytesToRead + " max message size is: " + this.maxMessageSize);
                        }

                        byte[] data = ReadThisAmmountOfBytes(bytesToRead);
                        DeveIncommingMessage devIn = new DeveIncommingMessage(null, data);
                        this.AddDeveIncommingMessage(devIn);

                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine("Socket exception: " + e.ToString());
                        break;
                    }
                    catch (EndOfStreamException e)
                    {
                        Console.WriteLine("Exception that happens when a client disconnects nice and safe: " + e.ToString());
                        break;
                    }
                    catch (InvalidDataException e)
                    {
                        Console.WriteLine("Invalid data exception: " + e.ToString());
                        break;
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("IOException: " + e.ToString());
                        break;
                    }
                }




                tcpClient.Close();
                isRunning = false;

                this.AddDeveIncommingMessage(new DeveIncommingMessage(null, new byte[2] { (byte)DeveMessageType.StatusChanged, (byte)NetworkStatus.Disconnected }));
            }
        }

        private byte[] ReadThisAmmountOfBytes(int bytesToRead)
        {
            MemoryStream mem = new MemoryStream();
            int nextReadCount = 0;
            int readCount = 0;
            while (bytesToRead > 0 && !shouldShutdown && tcpClient.Connected)
            {

                // Make sure we don't read beyond what the first message indicates
                //    This is important if the client is sending multiple "messages" --
                //    but in this sample it sends only one
                if (bytesToRead < receiveBuffer.Length)
                {
                    nextReadCount = bytesToRead;
                }
                else
                {
                    nextReadCount = receiveBuffer.Length;
                }


                // Read some data
                readCount = networkStream.Read(receiveBuffer, 0, nextReadCount);

                if (readCount == 0)
                {
                    throw new EndOfStreamException("Socket is eruit gekijlt, dit is goed en netjes :)");
                }

                // Display what we read
                //string readText = System.Text.Encoding.ASCII.GetString(receiveBuffer, 0, readCount);
                //Console.WriteLine("TCP Listener: Received: {0}", readText);
                mem.Write(receiveBuffer, 0, readCount);

                bytesToRead -= readCount;

            }
            lastKeepAlive = DateTime.Now;
            return mem.GetBuffer();
        }

        public void Send(DeveOutgoingMessage devOut)
        {
            messagesToSendQueue.Enqueue(devOut);
            //try
            //{
            //    NetworkStream networkStream = tcpClient.GetStream();
            //    Byte[] b = devOut.GetBytes();
            //    networkStream.Write(b, 0, b.Length);
            //    networkStream.Flush();

            //    lastKeepAlive = DateTime.Now;
            //}
            //catch
            //{
            //    Console.WriteLine("Client probably disconnected, message not send");
            //}
        }

        //public void SendByteDirectly(byte[] b)
        //{
        //    try
        //    {
        //        NetworkStream networkStream = tcpClient.GetStream();
        //        networkStream.Write(b, 0, b.Length);
        //        networkStream.Flush();

        //        lastKeepAlive = DateTime.Now;
        //    }
        //    catch
        //    {
        //        Console.WriteLine("Client probably disconnected, message not send");
        //    }
        //}

        public void CheckAndSendKeepAliveIfNeeded()
        {
            if (lastKeepAlive + keepAliveTimer < DateTime.Now)
            {
                //Console.WriteLine("Sending keepalive to server");
                lastKeepAlive = DateTime.Now;

                DeveOutgoingMessage keepalivemsg = new DeveOutgoingMessage(DeveMessageType.KeepAlive);
                Send(keepalivemsg);
            }
        }
    }
}
