using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace DragonMail
{
    class SmtpTransactions
    {


        public static void PacketParser(SmtpClient SmtpObject)
        {
            SmtpObject.LastActiveEpoch = MainClass.Epoch();

            byte[] data = new byte[SmtpObject.TcpClient.Available];
            SmtpObject.TcpClient.GetStream().Read(data, 0, data.Length);

            if (SmtpObject.CurrentMode == "Payload") { PayloadBuilder(SmtpObject, data); }

            if (data.Length < 4) { return; }
            
            string[] datadump = Encoding.UTF8.GetString(data).Split(' ');
            Console.Write(Encoding.UTF8.GetString(data));

            switch (datadump[0])
            {
                case "EHLO": 
                case "HELO":
                    {
                        if (datadump.Length < 2) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "501 Syntax: HELO hostname\r\n")); }
                        else
                        {
                            SmtpObject.ServiceDomain = datadump[1].Replace("\r", "").Replace("\n", "");
                            Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 " + MainClass.PrimaryDomain + "\r\n"));
                        }
                        break;
                    }
                //SMTP SPECIFIC

                case "MAIL":
                    {
                        if (datadump[1].Split(':')[0] != "FROM" || datadump.Length < 2 || (!datadump[1].Split(':')[1].Contains('@'))) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "501 Syntax: MAIL FROM:<address>\r\n")); break; }
                        else
                        {
                            SmtpObject.FromAddress = datadump[1].Split(':')[1].Replace(">", "").Replace("<", "").Replace("\r", "").Replace("\n", "");
                            SmtpObject.FromDomain = SmtpObject.FromAddress.Split('@')[1];
                            SmtpObject.CurrentMode = "From";
                            Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 2.1.0 Ok\r\n"));
                        }
                        break;
                    }
                case "RCPT":
                    {
                        if (datadump[1].Split(':')[0] != "TO" || datadump.Length < 2 || (!datadump[1].Split(':')[1].Contains('@'))) { Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "501 Syntax: RCPT TO:<address>\r\n")); break; }
                        else
                        {
                            string Email = datadump[1].Split(':')[1].Replace(">", "").Replace("<", "").Replace("\r", "").Replace("\n", "");
                            SmtpObject.ToAddress.Add(Email);
                            SmtpObject.ToDomain.Add(Email.Split('@')[1]);
                            SmtpObject.CurrentMode = "Rcpt";
                            Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 2.1.5 Ok\r\n"));
                        }
                        break;
                    }
                case "DATA":
                    {
                        Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "354 End data with <CR><LF>.<CR><LF>\r\n"));
                        SmtpObject.CurrentMode = "Payload";
                        break;
                    }
                case "AUTH": { break; }  // Any DragonMail Specific commands require the successful use of this command, otherwise other commands are a syntax errors, including failed attempts of this command. Additionally a Admin login is required. Mailbox logins will not work.
                case "RSET": { break; }
                case "VRFY": { break; }
                case "HELP": { break; }
                case "QUIT": {
                    Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "221 2.0.0 Bye\r\n"));
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

        public static void PayloadBuilder(SmtpClient SmtpObject, byte[] payload)
        {
            //Determine the size of the current packet.
            string[] datadump = Encoding.UTF8.GetString(payload).Split(' ');

            if (SmtpObject.payloadSize + payload.Length > MainClass.MaxEmailSize)
            {
                // Emil size/availible Disk space exceeded.
                // Notify client and close connection.
            }

            payload.CopyTo(SmtpObject.payLoadBuffer, SmtpObject.payloadSize + 1);

            SmtpObject.payloadSize += payload.Length;

            if (payload[SmtpObject.payloadSize - 4] == 0xD && payload[SmtpObject.payloadSize - 3] == 0xA && payload[SmtpObject.payloadSize - 2] == 0x2E && payload[SmtpObject.payloadSize - 1] == 0xD && payload[SmtpObject.payloadSize] == 0xA)
            {
                // Remove the last 3 Bytes from the payload.
                SmtpObject.payloadSize -= 3;
                Array.Copy(SmtpObject.payLoadBuffer, 0, SmtpObject.payLoad, 0, SmtpObject.payloadSize); //copy the email contents, minus the Smtp End-of-playload bytes
                //SmtpObject.payLoadBuffer
                Array.Clear(SmtpObject.payLoadBuffer, 0, SmtpObject.payLoadBuffer.Length);




                Task.Factory.StartNew(() => SmtpServices.Write(SmtpObject.TcpClient, "250 2.0.0 Ok: queued as " + SmtpObject.TransactionID));

                MailHandler.DeliverMail(SmtpObject);


            }
            

        }





    }
}
