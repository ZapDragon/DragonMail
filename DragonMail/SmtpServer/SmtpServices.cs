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
        public static List<TcpClient> Garbage = new List<TcpClient>();
        public static ulong CurrentTransactionID;

        public static long MaxEmailSize = 268435458;

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

                if (Garbage.Count > 0)
                {
                    foreach (TcpClient trash in Garbage)
                    {
                        SmtpClients.Remove(trash);
                    }
                    Garbage.Clear();
                }
                // Check Each SmtpServer object Listener for new connections and accept them.
                foreach (SmtpServer Listener in SmtpServers)
                {
                    if (Listener.SmtpListener.Pending()) { newSmtpClient(Listener.SmtpListener.AcceptTcpClient()); }
                }

                // Check Each SmtpClient object's Tcpobject for new incming data from clients and handle them.
                foreach (var KeyValue in SmtpClients)
                {
                    if (KeyValue.Key.Available > 0) {
                        //Update client's activity time.
                        KeyValue.Value.LastActiveEpoch = MainClass.Epoch();

                        if (KeyValue.Value.CurrentMode == "Payload")
                        {
                            
                            SmtpTransactions.PayloadBuilder(KeyValue.Value);
                            continue;
                        }
                        else
                        {
                            if (KeyValue.Key.Available < 4) { continue;  }
                            byte[] data = new byte[KeyValue.Key.Available];
                            KeyValue.Key.GetStream().Read(data, 0, data.Length);

                            string packet = Encoding.UTF8.GetString(data, 0, data.Length).Replace("\n", "").Replace("\r", "");
                            Console.WriteLine(packet);
                            SmtpTransactions.PacketParser(KeyValue.Value, packet.Split(' '));
                            if (KeyValue.Value.CurrentMode == "Payload") { continue; }
                        }
                    }
                    if (MainClass.Epoch() - 600 > KeyValue.Value.LastActiveEpoch) {
                        Task.Factory.StartNew(() => Write(KeyValue.Key, "421 4.4.2 " + MainClass.PrimaryDomain + " Error: timeout exceeded\r\n"));
                        KeyValue.Key.Close();
                        Garbage.Add(KeyValue.Key);
                    }
                }
            }
        }

        public static ulong GetTransactionID() { return CurrentTransactionID++; }

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
        public List<string> ToAddress = new List<string>();
        public List<string> ToDomain = new List<string>();
        public string ConnectingIP, FromAddress, FromDomain, ServiceDomain;
        public string CurrentMode;
        public byte[] payLoad;
        public byte[] payLoadBuffer = new byte[SmtpServices.MaxEmailSize];
        public long payloadSize = 0;
        public int LastActiveEpoch = MainClass.Epoch();
        public ulong TransactionID;
        public SmtpClient(TcpClient client)
        {
            TcpClient = client;
            TransactionID = SmtpServices.GetTransactionID();
            Console.WriteLine("New Smtp Client.");
        }
    }
}
