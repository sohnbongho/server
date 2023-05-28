using Akka.Actor;
using Akka.IO;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestLibrary;
using TestServer.World.UserInfo;

namespace TestServer.Socket
{       
    public class SessionActor : UntypedActor
    {
        public class UserToSessionLinkRequest
        {
            public IActorRef UserRef { get; set; } // 객체 연결
        }

        public class SendMessage
        {
            public GenericMessage Message { get; set; } 
        }        

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IActorRef _sessionCordiatorRef;
        private readonly string _remoteAddress;
        private readonly IActorRef _connectedSessionRef; // 연결 된 session 액터

        private IActorRef _userRef;                     // 


        public SessionActor(IActorRef sessionCordiator, string remoteAdress, IActorRef connection)
        {
            _sessionCordiatorRef = sessionCordiator;
            _remoteAddress = remoteAdress;
            _connectedSessionRef = connection;
            _userRef = null;
        }
        protected override void PreStart()
        {
            base.PreStart();
        }

        /// <summary>
        /// actor 종료
        /// </summary>
        protected override void PostStop()
        {
            _logger.Debug($"SessionActor.PostStop() :{_remoteAddress}");
            base.PostStop();

        }

        protected override void OnReceive(object message)
        {
            switch(message)
            {
                case UserToSessionLinkRequest request:
                    {
                        _userRef = request.UserRef; // 네트워크 세션과 User Actor 연결
                        break;
                    }
                case Tcp.Received received: // 메시지
                    {
                        _logger.Debug($"HandleMyMessage");
                        _userRef?.Tell(received, Self);

                        break;
                    }
                case SessionActor.SendMessage sendMessage:
                    {
                        _logger.Debug($"SendMessage ");
                        var binary = sendMessage.Message.ToByteArray();
                        _connectedSessionRef.Tell(Tcp.Write.Create(ByteString.FromBytes(binary)));                        

                        break;
                    }
                case SessionCordiatorActor.BroadcastMessage sendMessage:
                    {
                        _logger.Debug($"BroadcastMessage");                        
                        _sessionCordiatorRef.Tell(sendMessage);

                        break;
                    }
                case Tcp.ErrorClosed _:
                    {
                        _logger.Debug($"{Sender} closed");
                        // 연결 끈김
                        ClosedSocket();                        

                        break;
                    }
                case Tcp.PeerClosed closed:
                    {
                        _logger.Debug($"{Sender} PeerClosed ");                        
                        break;
                    }
                default:
                    {
                        Unhandled(message);
                        break;
                    }
            }            
        }

        /// <summary>
        /// socket이 끈김을 알림
        /// </summary>
        private void ClosedSocket()
        {
            _sessionCordiatorRef.Tell(new SessionCordiatorActor.ClosedRequest
            {
                RemoteAdress = _remoteAddress
            });
        }
    }

}
