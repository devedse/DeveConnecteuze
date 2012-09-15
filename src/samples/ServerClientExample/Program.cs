using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace Server
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());




            //Thread tr = new Thread(new ThreadStart(RunServerForm));
            //tr.Start();
            
            ////Wait 3 seconds
            //Thread.Sleep(3000);

            //Thread tr2 = new Thread(new ThreadStart(RunClientForm));
            //tr2.Start();

        }

        public static void RunClientForm()
        {
            ClientForm form = new ClientForm();
            Application.Run(form);
        }

        public static void RunServerForm()
        {
            ServerForm form = new ServerForm();
            Application.Run(form);
        }
    }
}
