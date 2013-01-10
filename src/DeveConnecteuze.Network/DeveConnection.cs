using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace DeveConnecteuze.Network
{
    public class DeveConnection
    {
        private TcpClient tcpClient;
        private DeveQueue<DeveOutgoingMessage> messagesToSendQueue = new DeveQueue<DeveOutgoingMessage>(10);
        private DateTime lastKeepAlive = DateTime.Now;
        private TimeSpan keepAliveTimer = new TimeSpan(0, 0, 5);
        private int bufferSize = 4096;
        private byte[] receiveBuffer;
        private NetworkStream networkStream;
        private DevePeer peer;
        private Boolean shouldShutdown = false;

        public DeveConnection(TcpClient tcpClient, DevePeer peer)
        {
            receiveBuffer = new byte[bufferSize];
            this.tcpClient = tcpClient;
            this.networkStream = tcpClient.GetStream();
            this.peer = peer;
        }

        internal void Start()
        {
            Thread tr = new Thread(Runner);
            tr.Start();
        }

        internal void Stop()
        {
            shouldShutdown = true;
            tcpClient.Close();
        }

        internal void Runner()
        {
            peer.AddDeveIncomingMessage(new DeveIncomingMessage(this, new byte[2] { (byte)DeveMessageType.StatusChanged, (byte)NetworkStatus.Connected }));

            Thread tr = new Thread(Sender);
            tr.Start();

            while (!shouldShutdown && tcpClient.Connected)
            {
                try
                {
                    byte[] firstLengthIntByteArray = ReadThisAmmountOfBytes(4);
                    int bytesToRead = BitConverter.ToInt32(firstLengthIntByteArray, 0);

                    if (bytesToRead > peer.MaxMessageSize)
                    {
                        Console.WriteLine("Warning: I'm gonna receive a big message of the size: " + bytesToRead);
                        //throw new InvalidDataException("This message is probably a bit big :), the size is: " + bytesToRead + " max message size is: " + this.maxMessageSize);
                    }

                    byte[] data = ReadThisAmmountOfBytes(bytesToRead);
                    DeveIncomingMessage devIn = new DeveIncomingMessage(this, data);
                    peer.AddDeveIncomingMessage(devIn);

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
                catch (IOException e)
                {
                    Console.WriteLine("IOException: " + e.ToString());
                    break;
                }
            }

            if (peer is DeveServer)
            {
                ((DeveServer)peer).RemoveClient(this);
            }

            peer.AddDeveIncomingMessage(new DeveIncomingMessage(this, new byte[2] { (byte)DeveMessageType.StatusChanged, (byte)NetworkStatus.Disconnected }));

        }

        internal void Sender()
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

        internal void CheckAndSendKeepAliveIfNeeded()
        {
            if (lastKeepAlive + keepAliveTimer < DateTime.Now)
            {
                //Console.WriteLine("Sending keepalive to server");
                lastKeepAlive = DateTime.Now;

                DeveOutgoingMessage keepalivemsg = new DeveOutgoingMessage(DeveMessageType.KeepAlive);
                Send(keepalivemsg);
            }
        }

        public void Send(DeveOutgoingMessage devOut)
        {
            messagesToSendQueue.Enqueue(devOut);
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
    }
}
