using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace DragonMail
{
    class SmtpServices
    {
        public static List<SmtpServer> SmtpServers = new List<SmtpServer>();

        public static void SmtpMain()
        {
            foreach (int Port in MainClass.Listeners)
            {
                SmtpServers.Add(new SmtpServer(Port));
            }

            Console.ReadKey();


        }
 
    }

    public class SmtpServer
    {
        TcpListener SmtpListener;
        public SmtpServer(int Port)
        {
            SmtpListener = new TcpListener(IPAddress.Parse(MainClass.ListenIP), Port);
            Console.WriteLine("Smtp Service Started on Port " + Port);
        }

    }
}
