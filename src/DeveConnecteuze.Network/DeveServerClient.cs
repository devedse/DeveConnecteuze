using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace DeveConnecteuze.Network
{
    public class DeveServerClient
    {
        private TcpClient tcpClient;
        public TcpClient TcpClient
        {
            get { return tcpClient; }
        }
        private NetworkStream networkStream;

        public NetworkStream NetworkStream
        {
            get { return networkStream; }
        }

        private Boolean shouldShutdown = false;
        private DeveServer deveServer;

        private DateTime lastKeepAlive = DateTime.Now;
        private TimeSpan keepAliveTimer = new TimeSpan(0, 0, 0, 1, 0);

        private DeveQueue<DeveOutgoingMessage> messagesToSendQueue = new DeveQueue<DeveOutgoingMessage>(10);


        private int size = 4096;
        private byte[] receiveBuffer;
        //private byte[] firstLengthIntByteArray = new byte[4];

        private String lastRemoteEndpoint = "Unknown";
        public String LastRemoteEndpoint
        {
            get { return lastRemoteEndpoint; }
        }

        public DeveServerClient(DeveServer deveServer, TcpClient tcpClient)
        {
            receiveBuffer = new byte[size];

            this.deveServer = deveServer;
            this.tcpClient = tcpClient;
            this.networkStream = tcpClient.GetStream();

            Thread tr = new Thread(new ThreadStart(Runner));
            tr.Start();

            Thread tr2 = new Thread(new ThreadStart(Sender));
            tr2.Start();
        }

        public void GoSendHandhakeTest()
        {
            using (var writer = new StreamWriter(networkStream))
            {
                writer.WriteLine("HTTP/1.1 101 WebSocket Protocol Handshake");
                writer.WriteLine("Upgrade: WebSocket");
                writer.WriteLine("Connection: Upgrade");
                writer.WriteLine("Sec-WebSocket-Origin: null");
                writer.WriteLine("Sec-WebSocket-Location: ws://10.33.183.48:2341/websession");
                writer.WriteLine("");
            }
            Thread.Sleep(1000);
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

        private void Runner()
        {

            //GoSendHandhakeTest();
            deveServer.AddDeveIncommingMessage(new DeveIncommingMessage(this, new byte[2] { (byte)DeveMessageType.StatusChanged, (byte)NetworkStatus.Connected }));

        
            try
            {
                lastRemoteEndpoint = this.tcpClient.Client.RemoteEndPoint.ToString();
            }
            catch
            {


            }

            //int nextReadCount;
            //int readCount;

            tcpClient.SendBufferSize = size;
            tcpClient.ReceiveBufferSize = size;

            while (!shouldShutdown && tcpClient.Connected)
            {
                try
                {
                    byte[] firstLengthIntByteArray = ReadThisAmmountOfBytes(4);
                    int bytesToRead = BitConverter.ToInt32(firstLengthIntByteArray, 0);

                    if (bytesToRead > deveServer.maxMessageSize)
                    {
                        Console.WriteLine("Warning: I'm gonna receive a big message of the size: " + bytesToRead);
                        //Debug.w.WriteLine("Warning: I'm gonna receive a big message of the size: " + bytesToRead);
                        //throw new InvalidDataException("This message is probably a bit big :), the size is: " + bytesToRead + " max message size is: " + deveServer.maxMessageSize);
                    }

                    byte[] data = ReadThisAmmountOfBytes(bytesToRead);
                    DeveIncommingMessage devIn = new DeveIncommingMessage(this, data);
                    deveServer.AddDeveIncommingMessage(devIn);

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



                //try
                //{
                //if (tcpClient.Available >= 4) //First int is available
                //{
                //    networkStream.Read(firstLengthIntByteArray, 0, firstLengthIntByteArray.Length);
                //    int bytesToRead = BitConverter.ToInt32(firstLengthIntByteArray, 0);

                //    if (bytesToRead > deveServer.maxMessageSize)
                //    {
                //        throw new InvalidDataException("This message is probably a bit big :), the size is: " + bytesToRead + " max message size is: " + deveServer.maxMessageSize);
                //        //Console.WriteLine("This message is probably a bit big :)");
                //        //break;
                //    }
                //    else
                //    {

                //        Byte[] totalMessage = new Byte[bytesToRead];
                //        MemoryStream mem = new MemoryStream(totalMessage);

                //        while (bytesToRead > 0)
                //        {

                //            // Make sure we don't read beyond what the first message indicates
                //            //    This is important if the client is sending multiple "messages" --
                //            //    but in this sample it sends only one
                //            if (bytesToRead < receiveBuffer.Length)
                //            {
                //                nextReadCount = bytesToRead;
                //            }
                //            else
                //            {
                //                nextReadCount = receiveBuffer.Length;
                //            }

                //            if (tcpClient.Available >= nextReadCount) //Next chunk of data is available, might not be needed
                //            {
                //                // Read some data
                //                readCount = networkStream.Read(receiveBuffer, 0, nextReadCount);

                //                // Display what we read
                //                //string readText = System.Text.Encoding.ASCII.GetString(receiveBuffer, 0, readCount);
                //                //Console.WriteLine("TCP Listener: Received: {0}", readText);
                //                mem.Write(receiveBuffer, 0, nextReadCount);

                //                bytesToRead -= readCount;
                //            }
                //        }

                //        DeveIncommingMessage devInc = new DeveIncommingMessage(this, totalMessage);
                //        deveServer.AddDeveIncommingMessage(devInc);
                //    }
                ////blocks until a client sends a message
                //bytesRead = networkStream.Read(message, 0, size);
                //Console.WriteLine("DeveServerClient MSG received");

                //if (bytesRead != 0)
                //{
                //    lastKeepAlive = DateTime.Now;

                //    byte[] b = new byte[bytesRead];
                //    Array.Copy(message, b, bytesRead);
                //    DeveIncommingMessage devInc = new DeveIncommingMessage(this, b);
                //    deveServer.AddDeveIncommingMessage(devInc);
                //}
                //else
                //{
                //    Console.WriteLine("An empty message was received... This happens if client disconnects");
                //    break;

                //}
                //}
                //else
                //{
                //    Thread.Sleep(1);
                //}




                //}
                //catch (SocketException eee)
                //{
                //    Console.WriteLine("Socket error: " + eee.ToString());
                //    break;
                //}
                //catch (InvalidDataException eee)
                //{
                //    Console.WriteLine("Exception: " + eee.ToString());
                //    break;
                //}


            }
            tcpClient.Close();

            deveServer.RemoveClient(this);
            deveServer.AddDeveIncommingMessage(new DeveIncommingMessage(this, new byte[2] { (byte)DeveMessageType.StatusChanged, (byte)NetworkStatus.Disconnected }));
        }

        public byte[] ReadThisAmmountOfBytes(int bytesToRead)
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
        }

        public void CheckAndSendKeepAliveIfNeeded()
        {
            if (lastKeepAlive + keepAliveTimer < DateTime.Now)
            {
                //Console.WriteLine("Sending keepalive to: " + tcpClient.Client.RemoteEndPoint);
                lastKeepAlive = DateTime.Now;

                DeveOutgoingMessage keepalivemsg = new DeveOutgoingMessage(DeveMessageType.KeepAlive);
                Send(keepalivemsg);
            }
        }

        public void Stop()
        {
            shouldShutdown = true;
            tcpClient.Close();
        }
    }
}
