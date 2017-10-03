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


        public static void PacketParser(SmtpClient SmtpObject, string[] command)
        {
            
            switch (command[0])
            {
                //case "EHLO": 
                case "HELO":
                    {
                        if (command.Length < 2) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "501 Syntax: HELO hostname\r\n")); }
                        else
                        {
                            SmtpObject.ServiceDomain = command[1];
                            Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 " + MainClass.PrimaryDomain + "\r\n"));
                        }
                        break;
                    }
                

                case "MAIL":
                    {
                        if (SmtpObject.ServiceDomain == null) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "503 5.5.1 Error: send HELO/EHLO first\r\n")); break; }
                        if (SmtpObject.FromDomain != null) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "503 5.5.1 Error: nested MAIL command\r\n")); break; }

                        if (command[1].Split(':')[0] != "FROM" || command.Length < 2 || (!command[1].Split(':')[1].Contains('@'))) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "501 Syntax: MAIL FROM:<address>\r\n")); break; }
                        else
                        {
                            SmtpObject.FromAddress = command[1].Split(':')[1].Replace(">", "").Replace("<", "");
                            SmtpObject.FromDomain = SmtpObject.FromAddress.Split('@')[1];
                            SmtpObject.CurrentMode = "From";
                            Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 2.1.0 Ok\r\n"));
                        }
                        break;
                    }
                case "RCPT":
                    {
                        if (SmtpObject.FromAddress == null) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "503 5.5.1 Error: need MAIL command\r\n")); break; }
                        if (command[1].Split(':')[0] != "TO" || command.Length < 2 || (!command[1].Split(':')[1].Contains('@'))) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "501 Syntax: RCPT TO:<address>\r\n")); break; }
                        else
                        {
                            string Email = command[1].Split(':')[1].Replace(">", "").Replace("<", "");

                            if (Email.Split('@')[1] != MainClass.PrimaryDomain || !MainClass.LocalMailBoxes.Contains(Email.Split('@')[0])) {
                                Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "554 5.7.1 " + command[1].Split(':')[0] + ": Relay access denied"));
                                break;
                            }
                            
                            SmtpObject.ToAddress.Add(Email);
                            SmtpObject.ToDomain.Add(Email.Split('@')[1]);
                            SmtpObject.CurrentMode = "Rcpt";
                            Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 2.1.5 Ok\r\n"));
                        }
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
                case "AUTH": { break; }  // This command will only be posible STARTLS has been initiated and complted. For the development of the server, TLS will not be deployed till later, likely in another revision.
                case "RSET": { break; }
                case "VRFY": { break; }
                case "HELP": { break; }
                case "QUIT": {
                    byte[] quitmsg = { 0x32, 0x32, 0x31, 0x20, 0x32, 0x2E, 0x30, 0x2E, 0x30, 0x20, 0x42, 0x79, 0x65, 0x0D, 0x0A };
                    SmtpObject.TcpClient.GetStream().Write(quitmsg, 0, 15);
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
            if (payload.Length < 4) { return; }
            //Determine the size of the current packet.
            

            if (SmtpObject.payloadSize + payload.Length > MainClass.MaxEmailSize)
            {
                // Emil size/availible Disk space exceeded.
                // Notify client and close connection.
            }

            payload.CopyTo(SmtpObject.payLoadBuffer, SmtpObject.payloadSize + 1);

            SmtpObject.payloadSize += payload.Length;



                if (payload[payload.Length - 5] == 13 && payload[payload.Length - 4] == 10 && payload[payload.Length - 3] == 46 && payload[payload.Length - 2] == 13 && payload[payload.Length - 1] == 10)
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
