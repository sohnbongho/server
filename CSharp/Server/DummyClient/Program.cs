using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DummyClient
{
    class GameSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected {endPoint}");

            // 보낸다.
            for (int i = 0; i < 5; i++)
            {
                byte[] sendBuff = Encoding.UTF8.GetBytes($"Hello World!{i}");
                Send(sendBuff);
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected {endPoint}");
        }

        // 이동 패킷 ((3,2)좌표로 이동하고 싶다!)
        // 15 3 2
        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Client]{recvData}");
            return buffer.Count;
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Tracnsterffed bytes: {numOfBytes}");
        }
    }
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

            connector.Connect(endPoint, () => { return new GameSession(); });

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
