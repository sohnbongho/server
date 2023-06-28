using ServerCore;
using System;
using System.Net;
using System.Threading;

namespace DummyClient
{
    
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(3000);

            // DNS(Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 8000);

            Connector connector = new Connector();

            connector.Connect(endPoint, () => { return new ServerSession(); });

            while (true)
            {

                try
                {
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                Thread.Sleep(100);

            }          
            
            
        }
    }
}
