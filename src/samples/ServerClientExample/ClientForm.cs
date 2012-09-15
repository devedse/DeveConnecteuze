using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using DeveConnecteuze.Network;


namespace Server
{
    public partial class ClientForm : Form
    {
        private DeveClient deveClient;
        private Boolean isRunning = true;

        public ClientForm()
        {
            InitializeComponent();

            int port = 23415;
            //int port = 10239;
            //deveClient = new DeveClient("82.176.123.30", port);

            //deveClient = new DeveClient("devedse.cloudapp.net", port);
            deveClient = new DeveClient("localhost", port);
            deveClient.Start();

            Thread tr = new Thread(new ThreadStart(Runner));
            tr.Start();
        }

        public void Runner()
        {
            while (isRunning)
            {
                DeveIncommingMessage im = deveClient.ReadMessage();

                if (im == null)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    switch (im.MessageType)
                    {
                        case DeveMessageType.Data:
                            String ding = im.ReadString();
                            this.Invoke(new MethodInvoker(delegate
                            {
                                this.listBox1.Items.Insert(0, ding);
                            }));
                            break;
                        case DeveMessageType.KeepAlive:
                            //doeniets :)
                            break;
                        case DeveMessageType.StatusChanged:
                            NetworkStatus status = (NetworkStatus)im.ReadByte();
                            Console.WriteLine("New network status: " + status);
                            break;
                        default:

                            break;
                    }



                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DeveOutgoingMessage devOut = new DeveOutgoingMessage();
            devOut.WriteString(textBox1.Text);
            deveClient.Send(devOut);
            textBox1.Text = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            isRunning = false;
            //Thread.Sleep(1000);
            deveClient.Stop();
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Thread tr = new Thread(GoSender);
            tr.IsBackground = true;
            tr.Start();
        }

        public void GoSender()
        {
            for (int i = 0; i < 1000; i++)
            {
                DeveOutgoingMessage msg = new DeveOutgoingMessage();
                msg.WriteString(i + "Hoi");
                deveClient.Send(msg);
                //Thread.Sleep(10);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            StringBuilder build = new StringBuilder();

            for (int i = 0; i < 999999; i++)
            {
                build.Append("S");
            }

            DeveOutgoingMessage msg = new DeveOutgoingMessage();
            msg.WriteString(":) " + build.ToString());
            deveClient.Send(msg);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog f = new OpenFileDialog();
            f.InitialDirectory = Directory.GetCurrentDirectory();
            DialogResult result = f.ShowDialog();

            switch (result)
            {
                case DialogResult.Abort:
                    break;
                case DialogResult.Cancel:
                    break;
                case DialogResult.Ignore:
                    break;
                case DialogResult.No:
                    break;
                case DialogResult.None:
                    break;
                case DialogResult.OK:
                    SendFile(f.FileName);
                    break;
                case DialogResult.Retry:
                    break;
                case DialogResult.Yes:
                    break;
                default:
                    break;
            }
        }

        public void SendFile(String fileName)
        {
            DeveOutgoingMessage msg = new DeveOutgoingMessage(DeveMessageType.Data);
            msg.WriteString("file");
            using (FileStream read = new FileStream(fileName, FileMode.Open))
            {
                msg.WriteString(Path.GetFileName(fileName));
                msg.WriteInt32((int)read.Length);
                byte[] b = new byte[read.Length];
                read.Read(b, 0, (int)read.Length);
                msg.WriteBytes(b);
            }
            deveClient.Send(msg);
        }
    }
}
