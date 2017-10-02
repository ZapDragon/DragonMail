using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace DragonMail
{
    class MainClass
    {
        // Readonly Byte Arrays.
        static readonly byte[] defaultConfig = { 0x00 };

        //Thread Handelers;
        public static Thread ImapServerThread;
        public static Thread SmtpServerThread;

        //Local Global Strings
        public static string[] ServerCFG;

        //Local Static ints
        public static long MaxEmailSize = 268435458;

        public static string PrimaryDomain, MailBoxPath, ListenIP;
        public static List<int> Listeners = new List<int>();

        static void Main()
        {
            Console.WriteLine("DragonMail Server written By Xylex Rayne 2017" + Environment.NewLine + " Loading Local config...");

            //Method used when local cfg file is missing.
            LoadDefaults();



            // Start Services
            //ImapServerThread = new Thread(new ThreadStart(ImapService.ImapThread));
            SmtpServerThread = new Thread(new ThreadStart(SmtpServices.SmtpMain));

            //ImapServerThread.Start();
            SmtpServerThread.Start();

        }
        static void LoadDefaults()
        {
            PrimaryDomain = "dragonstripes.net";
            MailBoxPath = "C:\\Mailboxes\\";
            ListenIP = "10.0.0.70";
            Listeners.Add(25);
            Listeners.Add(587);
            Console.WriteLine("Local config not found. Defaults loaded.");
        }

        public static int Epoch() { return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; }

    }
}
