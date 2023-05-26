using Akka.Actor;
using Akka.IO;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestLibrary;
using TestServer.Helper;
using TestServer.Socket;

namespace TestServer.World.UserInfo
{
    public class User 
    {
        public IActorRef WorldRef{ get; private set; } // 나를 포함하고 있는 월드
        public IActorRef SessionRef { get; private set; } // 원격지 Actor
        public IActorRef UserRef { get; private set; } // 내가 속해 있는 유저

        public static User Of(IUntypedActorContext context, IActorRef worldActor, IActorRef sessionRef)
        {
            var props = Props.Create(() => new UserActor(worldActor, sessionRef));
            var userActor = context.ActorOf(props, ActorPaths.User.Name);

            return new User(worldActor, sessionRef, userActor);
        }

        public User(IActorRef worldActor, IActorRef sessionRef, IActorRef userActor)
        {
            WorldRef = worldActor;
            SessionRef = sessionRef;
            UserRef = userActor;
        }
    }

    /// <summary>
    /// User Actor
    /// </summary>
    public class UserActor : ReceiveActor, ILogReceive
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public class RecvPacket
        {
            public IActorRef ConnectedSessionRef { get; set; }
            public GenericMessage MessageObject { get; set; }
        }
        public IActorRef WorldActor;
        public IActorRef SessionRef; // 원격지 Actor

        public UserActor(IActorRef worldActor, IActorRef sessionRef)
        {
            WorldActor = worldActor;
            SessionRef = sessionRef;
            sessionRef.Tell(new SessionActor.UserToSessionLinkRequest
            {
                UserRef = Self
            });

            Receive<UserActor.RecvPacket>(
             message =>
             {
                 OnRecvPacket(message);
             });

        }
        private void OnRecvPacket(UserActor.RecvPacket packet)
        {
            var messageObject = packet.MessageObject;
            var clientSession = packet.ConnectedSessionRef;
            switch (messageObject)
            {
                case SayRequest sayRequest:
                    {
                        _logger.Debug($"SayRequest - {sayRequest.UserName} : {sayRequest.Message}");

                        var res = new SayResponse
                        {
                            UserName = sayRequest.UserName,
                            Message = sayRequest.Message
                        };
                        var binary = res.ToByteArray();
                        clientSession.Tell(Tcp.Write.Create(ByteString.FromBytes(binary)));

                        break;
                    }
            }
        }

    }
}
