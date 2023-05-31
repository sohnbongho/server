using System;
using System.Linq;
using System.Threading;
using Akka.Actor;
using Akka.Configuration;

namespace TestClient
{
    /// <summary>
    /// 프로그램
    /// </summary>
    class Program
    {   

        /// <summary>
        /// 프로그램 시작하기
        /// </summary>
        //private static void Main()
        //        {
        //            string hocon = @"
        //akka
        //{  
        //    actor
        //    {
        //        provider = remote
        //    }
        //    remote
        //    {
        //        dot-netty.tcp
        //        {
        //            port     = 0
        //            hostname = localhost
        //        }
        //    }
        //}
        //";

        //            Config config = ConfigurationFactory.ParseString(hocon);

        //            using(ActorSystem actorSystem = ActorSystem.Create("TestClient", config))
        //            {
        //                var threadId = Thread.CurrentThread.ManagedThreadId;
        //                var userName = $"Roggan{threadId}";

        //                var props = Props.Create(() => new ChatClientActor(userName));                                
        //                IActorRef actorRef = actorSystem.ActorOf(props, "clientActor");
        //                var actorName = actorRef.ToString();
        //                Console.WriteLine($"Actor Name:{actorName}");

        //                actorRef.Tell(new ConnectRequest() {                    
        //                    UserName = $"Roggan{threadId }:",
        //                });

        //                while(true)
        //                {
        //                    string input = Console.ReadLine();

        //                    if(input.StartsWith("/"))
        //                    {
        //                        string[] partArray = input.Split(' ');
        //                        string   command   = partArray[0].ToLowerInvariant();
        //                        string   rest      = string.Join(" ", partArray.Skip(1));

        //                        if(command == "/nick")
        //                        {
        //                            actorRef.Tell(new NickNameRequest { NewUserName = rest });
        //                        }

        //                        if(command == "/exit")
        //                        {
        //                            Console.WriteLine("exiting");

        //                            break;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        actorRef.Tell(new SayRequest() { Message = input });
        //                    }
        //                }

        //                actorSystem.Terminate().Wait();
        //            }
        //        }        
        private static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("MySystem"))
            {
                var clientProps = Props.Create(() => new TelnetClient("127.0.0.1", 8081));
                var client = system.ActorOf(clientProps, "TelnetClient");

                while (true)
                {
                    var input = Console.ReadLine();
                    if (input.Equals("exit"))
                        break;

                    client.Tell(input);
                }

                system.Terminate().Wait();
            }
        }
    }
}