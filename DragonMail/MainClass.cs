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
        public static List<string> LocalMailBoxes = new List<string>();

        //Local Bools
        public static bool CatchAll = true;

        //Local Static ints
        public static long MaxEmailSize = 268435458;

        public static string PrimaryDomain, MailBoxPath, ListenIP, CatchallMailbox;
        public static List<int> Listeners = new List<int>();

        static void Main()
        {
            LocalMailBoxes.Add("abuse");
            LocalMailBoxes.Add("amazon");
            LocalMailBoxes.Add("hostmaster");
            LocalMailBoxes.Add("paypal");
            LocalMailBoxes.Add("postmaster");
            LocalMailBoxes.Add("webmaster");
            LocalMailBoxes.Add("xylexrayne");

            Console.WriteLine("DragonMail Server written By Xylex Rayne 2017" + Environment.NewLine + "Loading Local config...");

            //Method used when local cfg file is missing.
            LoadDefaults();

            PrimeDirectories();


            // Start Services
            //ImapServerThread = new Thread(new ThreadStart(ImapService.ImapThread));
            SmtpServerThread = new Thread(new ThreadStart(SmtpServices.SmtpMain));

            //ImapServerThread.Start();
            SmtpServerThread.Start();

        }
        static void LoadDefaults()
        {
            //PrimaryDomain = ""; // Single Domain only. For now the server can only accept mail addressed to mailboxes at this domain.
            //MailBoxPath = ""; //Any Absolute Localized path. D:\\Mailboxes\\ or /var/mailboxes/ Trailing slash is a must.
            //ListenIP = ""; A single IPv4 address to listen on.
            PrimaryDomain = "dragonstripes.net";
            MailBoxPath = "D:\\Mailboxes\\";
            CatchallMailbox = "xylexrayne";
            ListenIP = "144.217.40.133";
            //ListenIP = "10.0.0.70";
            Listeners.Add(25);
            //Listeners.Add(587);
            Console.WriteLine("Local config not found. Defaults loaded.\r\n");
        }

        static void PrimeDirectories()
        {
            if (!Directory.Exists(MailBoxPath))
            {
                try { Directory.CreateDirectory(MailBoxPath); }
                catch
                {
                    Console.WriteLine("MailBox directory doesnt exist, and Could Not be created. Shutting down mail server.");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
            }

            foreach (string Mailbox in LocalMailBoxes)
            {
               if (!Directory.Exists(MailBoxPath + Mailbox)) {
                    try { Directory.CreateDirectory(MainClass.MailBoxPath + Mailbox); }
                    catch {
                        Console.WriteLine("Unable to create local Mailbox for " + Mailbox + ". Shutting down.");

                    }
                    }
                }
                
            }

    
        public static int Epoch() { return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; }
        }

    }

