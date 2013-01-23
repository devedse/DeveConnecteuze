using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using DeveConnecteuze.Network;

namespace Server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public void Ogo()
        {

            TcpListener tcpListener;
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 23415);
            tcpListener = new TcpListener(ip);
            tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = tcpListener.AcceptTcpClient();
                Console.WriteLine("Client connected");

                //create a thread to handle communication
                //with connected client


                NetworkStream clientStream = client.GetStream();

                int size = 50000000;
                byte[] message = new byte[size];
                int bytesRead;
                while (true)
                {




                    bytesRead = 0;

                    try
                    {
                        //blocks until a client sends a message
                        bytesRead = clientStream.Read(message, 0, size);
                        Console.WriteLine("MSG RECEIVED!!! :)");
                    }
                    catch
                    {
                        //a socket error has occured
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        //the client has disconnected from the server
                        break;
                    }



                    //List<byte> delijst = new List<byte>();
                    //delijst.AddRange(message);

                    //byte[] byteint = { delijst[0], delijst[1], delijst[2], delijst[3] };
                    int lengthString = BitConverter.ToInt32(message, 0);

                    //String aaa = BitConverter.ToString(message, 4, lengthString);

                    //message has successfully been received
                    ASCIIEncoding encoder = new ASCIIEncoding();
                    String msg = "";
                    msg = encoder.GetString(message, 4, lengthString);
                    //System.Diagnostics.Debug.WriteLine(msg);

                    List<byte> lijstje = new List<byte>();
                    lijstje.AddRange(message);
                    for (int i = 0; i < lengthString + 4; i++)
                    {
                        lijstje.RemoveAt(0);
                    }

                    Byte[] a = lijstje.ToArray();

                    File.WriteAllBytes(msg, a);

                    if (msg == "Hello")
                    {


                        byte[] buffer = encoder.GetBytes("Hello Client!");

                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }


                }


                //Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                //clientThread.Start(client);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            ServerForm serverForm = new ServerForm();
            serverForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ClientForm clientForm = new ClientForm();
            clientForm.Show();
        }
    }
}
