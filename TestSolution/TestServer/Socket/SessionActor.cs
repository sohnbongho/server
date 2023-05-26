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

        }

        /// <summary>
        /// actor 종료
        /// </summary>
        protected override void PostStop()
        {
            _logger.Debug($"SessionActor.PostStop() :{_remoteAddress}");

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
                        HandleReceived(received);
                        break;
                    }
                case Tcp.ErrorClosed _:
                    {
                        // 연결 끈김
                        _logger.Debug($"{Sender} closed");
                        _sessionCordiatorRef.Tell(new SessionCordiatorActor.Delete
                        {
                            RemoteAdress = _remoteAddress
                        });

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
        /// 메시지를 받는 부분
        /// TODO: switch-case 문이 아닌 좀더 심플한 방법이 필요
        /// </summary>
        /// <param name="received"></param>
        /// <returns></returns>
        private bool HandleReceived(Tcp.Received received)
        {
            _logger.Debug($"HandleMyMessage");

            // 받은 패킷을 유저 actor에 보낸다.
            var messageObject = GenericMessage.FromByteArray(received.Data.ToArray());
            var recvMessage = new UserActor.RecvPacket
            {
                ConnectedSessionRef = _connectedSessionRef,
                MessageObject = messageObject
            };

            _userRef?.Tell(recvMessage, Self);            
            return true;
        }
    }

}
