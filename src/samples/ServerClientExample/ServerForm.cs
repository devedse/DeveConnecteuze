using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DeveConnecteuze.Network;
using System.Threading;
using System.IO;

namespace Server
{
    public partial class ServerForm : Form
    {
        private DeveServer deveServer;
        private Boolean isRunning = true;

        public ServerForm()
        {
            InitializeComponent();

            int port = 23415;
            //int port = 10239;
            deveServer = new DeveServer(port);
            deveServer.Start();

            Thread tr = new Thread(new ThreadStart(Runner));
            tr.Start();
        }

        public void Runner()
        {
            while (isRunning)
            {
                DeveIncomingMessage im = deveServer.ReadMessage();

                if (im == null)
                {
                    Thread.Sleep(1);
                }
                else
                {

                    switch (im.MessageType)
                    {
                        case DeveMessageType.Data:
                            try
                            {
                                String ding = im.ReadString();

                                if (ding.Equals("file"))
                                {
                                    String filename = im.ReadString();
                                    int fileSize = im.ReadInt32();
                                    byte[] fileData = im.ReadBytes(fileSize);

                                    this.Invoke(new MethodInvoker(delegate
                                    {
                                        this.listBox1.Items.Insert(0, "File ontvangen: " + filename + " writing...");
                                    }));

                                    using (FileStream stream = new FileStream(filename, FileMode.Create))
                                    {
                                        stream.Write(fileData, 0, fileSize);
                                    }

                                    this.Invoke(new MethodInvoker(delegate
                                    {
                                        this.listBox1.Items.Insert(0, "Done writing file");
                                    }));
                                }
                                else if (ding.Equals("teststuff"))
                                {
                                    this.Invoke(new MethodInvoker(delegate
                                    {
                                        this.listBox1.Items.Insert(0, "Just received a teststuff message: " + im.ReadFloat() + " and " + im.ReadUInt32());
                                    }));

                                }
                                else
                                {

                                    DeveOutgoingMessage outje = new DeveOutgoingMessage();
                                    outje.WriteString("Dit kreeg ik net: " + ding);
                                    deveServer.SendToAll(outje);

                                    //Console.WriteLine(ding);

                                    this.Invoke(new MethodInvoker(delegate
                                    {
                                        this.listBox1.Items.Insert(0, ding);
                                    }));
                                }
                            }
                            catch (Exception ezxz)
                            {
                                this.Invoke(new MethodInvoker(delegate
                                {
                                    this.listBox1.Items.Insert(0, ezxz.ToString());
                                }));
                            }
                            break;
                        case DeveMessageType.KeepAlive:
                            //doeniets :)
                            break;
                        case DeveMessageType.StatusChanged:
                            NetworkStatus status = (NetworkStatus)im.ReadByte();
                            Console.WriteLine("New network status voor " + im.Sender + ": " + status);
                            break;
                        default:

                            break;
                    }


                }
            }

            this.Invoke(new MethodInvoker(delegate
            {
                this.Close();
            }));


        }

        private void button1_Click(object sender, EventArgs e)
        {
            isRunning = false;
            //Thread.Sleep(1000);
            deveServer.Stop();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Invoke(new MethodInvoker(delegate
            {
                this.listBox2.Items.Clear();
                lock (deveServer.Clients)
                {
                    foreach (DeveConnection deveClientConnection in deveServer.Clients)
                    {
                        this.listBox2.Items.Add(deveClientConnection);
                    }
                }
            }));

        }
    }
}
