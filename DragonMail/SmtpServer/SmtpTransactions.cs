using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;

namespace DragonMail
{
    class SmtpTransactions
    {


        public static void PacketParser(SmtpClient SmtpObject, string Packet)
        {
            string[] SpaceSplit = Packet.Split(' '); //Having these two string arrays seems redundant, but having two points in which to parse commands and their arguments cuts down on code drastically.
            string[] ColonSplit = Packet.Split(':');

            switch (SpaceSplit[0])
            {
                case "AUTH": { break; }  // This command will only be posible when STARTLS has been initiated and complted. For the development of the server, TLS will not be deployed till later, likely in another revision.
                case "VRFY": { break; }
                case "HELP": { break; }
                //case "EHLO": 
                case "HELO":
                    {
                        if (SpaceSplit.Length < 2) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "501 Syntax: HELO hostname\r\n")); }
                        else
                        {
                            SmtpObject.ServiceDomain = SpaceSplit[1];
                            Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 " + MainClass.PrimaryDomain + "\r\n"));
                        }
                        break;
                    }
                case "MAIL":
                    {
                        if (SmtpObject.ServiceDomain == null) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "503 5.5.1 Error: send HELO/EHLO first\r\n")); break; }
                        if (SmtpObject.FromAddress != null) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "503 5.5.1 Error: nested MAIL command\r\n")); break; }
                        string Email = ColonSplit[1].Replace(">", "").Replace("<", "").Replace(" ", "");
                        if (SpaceSplit.Length < 2 || (!Email.Contains('@'))) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "501 Syntax: MAIL FROM:<address>\r\n")); break; }
                        SmtpObject.FromAddress = Email;
                        SmtpObject.FromDomain = Email.Split('@')[1];
                        Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 2.1.0 Ok\r\n"));
                        break;
                    }
                case "RCPT":
                    {
                        if (SmtpObject.FromAddress == null) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "503 5.5.1 Error: need MAIL command\r\n")); break; }
                        string Email = ColonSplit[1].Replace(">", "").Replace("<", "").Replace(" ", "");
                        if (SpaceSplit.Length < 2 || (!Email.Contains('@'))) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "501 Syntax: RCPT TO:<address>\r\n")); break; }
                        if (Email.Split('@')[1] != MainClass.PrimaryDomain) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "554 5.7.1 " + Email + " : Relay access denied\r\n")); break; }

                        if (!MainClass.LocalMailBoxes.Contains(Email.Split('@')[0]))
                        {
                            if (!MainClass.CatchAll)
                            {
                                Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "550 5.1.1 " + SpaceSplit[1].Split(':')[0] + ": Bad destination mailbox address\r\n"));
                                break;
                            }
                            else
                            {
                                SmtpObject.ToAddress.Add(MainClass.CatchallMailbox + "@" + MainClass.PrimaryDomain);
                                SmtpObject.ToDomain.Add(MainClass.PrimaryDomain);
                            }
                        }
                        else
                        {
                            SmtpObject.ToAddress.Add(Email);
                            SmtpObject.ToDomain.Add(Email.Split('@')[1]);
                        }
                        Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 2.1.5 Ok\r\n"));
                        break;
                    }
                case "DATA":
                    {
                        if (SmtpObject.ToAddress.Count < 1) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "503 5.5.1 Error: need RCPT command\r\n")); break; }
                        if (SmtpObject.FromAddress == null) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "503 5.5.1 Error: need MAIL command\r\n")); break; }
                        Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "354 End data with <CR><LF>.<CR><LF>\r\n"));
                        SmtpObject.CurrentMode = "Payload";
                        break;
                    }
                case "RSET": {
                    SmtpObject.FromAddress = null;
                    SmtpObject.FromDomain = null;
                    SmtpObject.payloadSize = 0;
                    SmtpObject.ToAddress.Clear();
                    SmtpObject.ToDomain.Clear();
                    Array.Clear(SmtpObject.payLoad, 0, SmtpObject.payLoad.Length);
                    Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 2.0.0 Ok\r\n"));
                    break;
                }
                case "QUIT": {
                    byte[] quitmsg = { 0x32, 0x32, 0x31, 0x20, 0x32, 0x2E, 0x30, 0x2E, 0x30, 0x20, 0x42, 0x79, 0x65, 0x0D, 0x0A };
                    SmtpObject.TcpClient.GetStream().Write(quitmsg, 0, 15);
                    Console.WriteLine("Closed Connection from: " + SmtpObject.ConnectingIP);
                    SmtpObject.TcpClient.Close();
                    SmtpServices.Garbage.Add(SmtpObject.TcpClient);
                    break;
                }


                //case "\r\n": { break; }
                default:
                    {
                        Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "500 Syntax error, command unrecognised\r\n"));
                        break;
                    }
            }

        }

        public static void PayloadBuilder(SmtpClient SmtpObject)
        {
            byte[] payload = new byte[SmtpObject.TcpClient.Available];
            SmtpObject.TcpClient.GetStream().Read(payload, 0, payload.Length);
            payload.CopyTo(SmtpObject.payLoadBuffer, SmtpObject.payloadSize + 1);
            SmtpObject.payloadSize += payload.Length;
            if (SmtpObject.payloadSize + payload.Length > MainClass.MaxEmailSize)
            {
                // Emil size/availible Disk space exceeded.
                // Notify client and close connection.
            }
            if (SmtpObject.payloadSize < 5) { return; } // Is the payload even big enough to check if its ended?

            byte[] payloadEnd = new byte[5];
            byte[] End = { 0x0D, 0x0A, 0x2E, 0x0D, 0x0A };
            Array.Copy(SmtpObject.payLoadBuffer, SmtpObject.payloadSize - 4, payloadEnd, 0, 5);
            if (End.SequenceEqual(payloadEnd))
            //if (SmtpObject.payLoadBuffer[SmtpObject.payloadSize - 5] == 13 && SmtpObject.payLoadBuffer[SmtpObject.payloadSize - 4] == 10 && SmtpObject.payLoadBuffer[SmtpObject.payloadSize - 3] == 46 && SmtpObject.payLoadBuffer[SmtpObject.payloadSize - 2] == 13 && SmtpObject.payLoadBuffer[SmtpObject.payloadSize - 1] == 10)
                {
                    SmtpObject.CurrentMode = "Completed Transaction";
                    // Remove the last 3 Bytes from the payload.
                    SmtpObject.payloadSize -= 3;
                    SmtpObject.payLoad = new byte[SmtpObject.payloadSize];
                    Array.Copy(SmtpObject.payLoadBuffer, 0, SmtpObject.payLoad, 0, SmtpObject.payloadSize); //copy the email contents, minus the Smtp End-of-playload bytes
                    //SmtpObject.payLoadBuffer
                    Array.Clear(SmtpObject.payLoadBuffer, 0, SmtpObject.payLoadBuffer.Length);

                    Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 2.0.0 Ok: queued as " + SmtpObject.TransactionID + "\r\n"));

                    MailHandler.DeliverMail(SmtpObject);
                }
        }
    }
}
