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
        public static Dictionary<TcpClient, SmtpClient> SmtpClients = new Dictionary<TcpClient, SmtpClient>();

        public static void SmtpMain()
        {
            // Create a new Smtp Listener object for ever listener port specified, and start the service.
            foreach (int Port in MainClass.Listeners) {
                SmtpServer SmtpService = new SmtpServer(Port);
                SmtpServers.Add(SmtpService);
                SmtpService.SmtpListener.Start();
                    
            }

            // Begining Main service loop.
            while (true)
            {
                
                // Check Each SmtpServer object Listener for new connections and accept them.
                foreach (SmtpServer Listener in SmtpServers)
                {
                    if (Listener.SmtpListener.Pending()) { newSmtpClient(Listener.SmtpListener.AcceptTcpClient()); }
                }

            }


        }

        public static void newSmtpClient(TcpClient client)
        {
            string address = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

            SmtpClient clientObject = new SmtpClient(client);
            SmtpClients.Add(client, clientObject);

            clientObject.ConnectingIP = address;
            clientObject.CurrentMode = "Login";
            Task.Factory.StartNew(() => Write(client, "220 " + MainClass.PrimaryDomain + Environment.NewLine));
        }


        public static async Task Write(TcpClient client, string Data)
        {
            byte[] packet = Encoding.UTF8.GetBytes(Data);
            await client.GetStream().WriteAsync(packet, 0, packet.Length);
        }



    }

    public class SmtpServer
    {
        public TcpListener SmtpListener;
        public SmtpServer(int Port)
        {
            SmtpListener = new TcpListener(IPAddress.Parse(MainClass.ListenIP), Port);
            Console.WriteLine("Smtp Service Started on Port " + Port);
        }

    }

    public class SmtpClient
    {
        public TcpClient TcpClient;
        public string ConnectingIP;
        public string CurrentMode;
        public SmtpClient(TcpClient client)
        {
            TcpClient = client;
            Console.WriteLine("New Smtp Client.");
        }
    }
}
