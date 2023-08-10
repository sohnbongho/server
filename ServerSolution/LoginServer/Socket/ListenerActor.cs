using Akka.Actor;
using Akka.IO;
using log4net;
using System.Net;
using System.Reflection;
using System.Net.Sockets;
using LoginServer.Helper;

namespace LoginServer.Socket
{    
    public class ListenerActor : UntypedActor
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly int _port;
                
        private IActorRef _sessionCordiatorRef = null;
        private IActorRef _worldActorRef = ActorRefs.Nobody;
        private IActorRef _tcpListener = ActorRefs.Nobody;

        public static IActorRef ActorOf(ActorSystem actorSystem, IActorRef worldActor, int port)
        {   
            var clientProps = Props.Create(() => new ListenerActor(worldActor, port));
            return actorSystem.ActorOf(clientProps, ActorPaths.Listener.Name);
        }

        public ListenerActor(IActorRef worldActor, int port)
        {
            _worldActorRef = worldActor;
            _port = port;            

            Context.System.Tcp().Tell(new Tcp.Bind(Self, new IPEndPoint(IPAddress.Any, port)));            
        }
        protected override void PreStart()
        {
            base.PreStart();

            // 세션을 관리해 주는 Actor 생성            
            _sessionCordiatorRef = SessionCordiatorActor.ActorOf(Context, Self, _worldActorRef);            
        }

        protected override void PostStop()
        {
            _logger.Info($"PostStop ListenerActor");

            _tcpListener.Tell(Tcp.Unbind.Instance, Self);
            _tcpListener.Tell(Tcp.Closed.Instance, Self);

            base.PostStop();
        }
        protected override void OnReceive(object message)
        {
            switch(message)
            {
                case Tcp.Bound bound:
                    {
                        _tcpListener = Sender;

                        _logger.Info($"Listening on {bound.LocalAddress}");
                        break;
                    }
                case Tcp.Connected connected:
                    {
                        _logger.Info($"Tcp.Connected on {connected.RemoteAddress.ToString()}");

                        _sessionCordiatorRef?.Tell(new SessionCordiatorActor.RegisteredRequest{
                            Sender = Sender,
                            RemoteAdress = connected.RemoteAddress.ToString(),
                        });

                        break;
                    }
                case DeathPactException:
                    {
                        break;
                    }
                case Terminated terminated:
                    {
                        // Here, handle the termination of the watched actor.
                        // For example, you might want to create a new actor or simply log the termination.
                        break;
                    }
                default:
                    {
                        Unhandled(message);
                        break;
                    }
            }            
        }
    }

}
