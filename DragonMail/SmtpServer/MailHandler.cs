using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DragonMail
{
    class MailHandler
    {
        public static List<string> LocalMailBoxes = new List<string>();
        public static async Task DeliverMail(SmtpClient SmtpObject)
        {
                foreach (string address in SmtpObject.ToAddress)
                {
                    if (address.Split('@')[1] == MainClass.PrimaryDomain)
                    {
                        LocalMailBoxes.Add(address.Split('@')[0]);
                    }
                }


            if (!Directory.Exists(MainClass.MailBoxPath))
            {
                try { Directory.CreateDirectory(MainClass.MailBoxPath); }
                catch {
                    Console.WriteLine("MailBox directory doesnt exist, and Could Not be created. Pending mail will be lost. SHutting down mail server.");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
                try { 
                    foreach (string Mailbox in LocalMailBoxes) {
                        Directory.CreateDirectory(MainClass.MailBoxPath + Mailbox);
                    }
                }
                catch { return; }

                try {
                    foreach (string Mailbox in LocalMailBoxes)
                    {
                        File.Create(MainClass.MailBoxPath + Mailbox + "\\" + MainClass.Epoch() + "." + SmtpObject.TransactionID + "_eml" );
                        File.WriteAllBytes(MainClass.MailBoxPath + Mailbox + "\\" + MainClass.Epoch() + "." + SmtpObject.TransactionID + "_eml", SmtpObject.payLoad);
                    }
                }
                catch { return; }
                SmtpObject.ToAddress.Clear();
                SmtpObject.ToDomain.Clear();
                SmtpObject.FromAddress = "";
                SmtpObject.FromDomain = "";
                Array.Clear(SmtpObject.payLoad, 0, SmtpObject.payLoad.Length);
            }

        }
    }
}
