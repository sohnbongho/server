using Akka.Actor;
using Akka.IO;
using Google.Protobuf;
using log4net;
using Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            public MessageWrapper Message { get; set; } 
        }        

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IActorRef _sessionCordiatorRef;
        private readonly string _remoteAddress;
        private readonly IActorRef _connectedSessionRef; // 연결 된 session 액터
        
        private IActorRef _userRef;                     // 

        // TCP 특성상 다 오지 못 해, 버퍼가 쌓으면서 다 받으면 가져간다.
        private List<byte> _buffer = new List<byte>();
        private int? _currentMessageLength = null;
        private const int _maxRecvLoop = 100; // 패킷받는 최대 카운트


        public SessionActor(IActorRef sessionCordiator, string remoteAdress, IActorRef connection)
        {
            _sessionCordiatorRef = sessionCordiator;
            _remoteAddress = remoteAdress;
            _connectedSessionRef = connection;
            _userRef = null;
        }
        protected override void PreStart()
        {
            _logger.Debug($"SessionActor.PreStart() :{_remoteAddress}");
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

                        _buffer.AddRange(received.Data.ToArray());

                        // Loop while we might still have complete messages to process
                        for(var i = 0; i < _maxRecvLoop; ++i)
                        {
                            // If we don't know the length of the message yet (4 byte, int)
                            if (!_currentMessageLength.HasValue)
                            {
                                if (_buffer.Count < sizeof(int))
                                    return;

                                _currentMessageLength = BitConverter.ToInt32(_buffer.ToArray(), 0);
                                _buffer.RemoveRange(0, sizeof(int));
                            }

                            // If entire message hasn't been received yet
                            if (_buffer.Count < _currentMessageLength.Value)
                                return;

                            var messageBytes = _buffer.GetRange(0, _currentMessageLength.Value).ToArray();
                                                        
                            _buffer.RemoveRange(0, _currentMessageLength.Value);
                            _currentMessageLength = null;

                            // Handle the message
                            _userRef?.Tell(new UserActor.SessionReceiveData
                            {
                                RecvBuffer = messageBytes,
                            }, Self);
                        }

                        break;
                    }
                case SessionActor.SendMessage sendMessage:
                    {
                        _logger.Debug($"SendMessage ");                        
                        
                        var binary = sendMessage.Message.ToByteArray();
                        int buffSize = binary.Length;

                        byte[] byteArray = null;
                        using (var stream = new MemoryStream())
                        {
                            using (var writer = new BinaryWriter(stream))
                            {
                                writer.Write(buffSize); // size는 int으로
                                writer.Write(binary);
                                byteArray = stream.ToArray();
                            }
                        }

                        if(byteArray != null)
                            _connectedSessionRef.Tell(Tcp.Write.Create(Akka.IO.ByteString.FromBytes(byteArray)));

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
                        // 연결 끈김
                        ClosedSocket();
                        break;
                    }
                default:
                    {
                        _logger.Debug($"{Sender} Unhandled");
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
