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
        public static void DeliverMail(SmtpClient SmtpObject)
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

                        Console.WriteLine("MailBox directory doesnt exist. Pending mail will be lost. Shutting down mail server.");
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }
                
                else
                {
                    try
                    {
                        foreach (string Mailbox in LocalMailBoxes)
                        {
                            File.WriteAllBytes(MainClass.MailBoxPath + Mailbox + "\\" + MainClass.Epoch() + "." + SmtpObject.TransactionID + "_eml", SmtpObject.payLoad);
                        }
                    }
                    catch { Console.WriteLine("Mailbox delivery error"); }
                    LocalMailBoxes.Clear();
                    SmtpObject.ToAddress.Clear();
                    SmtpObject.ToDomain.Clear();
                    SmtpObject.FromAddress = "";
                    SmtpObject.FromDomain = "";
                    SmtpObject.CurrentMode = "Completed Delivery";
                    Array.Clear(SmtpObject.payLoad, 0, SmtpObject.payLoad.Length);
                }
        }
    }
}
