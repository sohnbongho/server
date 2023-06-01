using Akka.Actor;
using Akka.IO;
using log4net;
using System.Net;
using System.Reflection;
using TestServer.Helper;
using TestServer.World;

namespace TestServer.Socket
{    
    public class ListenerActor : UntypedActor
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly int _port;

        private IActorRef _worldActorRef;
        private IActorRef _sessionCordiatorRef = null;

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
            // 세션을 관리해 주는 Actor 생성            
            _sessionCordiatorRef = SessionCordiatorActor.ActorOf(Context, Self, _worldActorRef);
            ActorSupervisorHelper.Instance.SetSessionCordiatorRef(_sessionCordiatorRef);

            base.PreStart();
        }

        protected override void PostStop()
        {
            base.PostStop();
        }
        protected override void OnReceive(object message)
        {
            switch(message)
            {
                case Tcp.Bound bound:
                    {
                        _logger.Info($"Listening on {bound.LocalAddress}");
                        break;
                    }
                case Tcp.Connected connected:
                    {
                        _sessionCordiatorRef?.Tell(new SessionCordiatorActor.RegisteredRequest{
                            Sender = Sender,
                            RemoteAdress = connected.RemoteAddress.ToString(),
                        });

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
