using Akka.Actor;
using Akka.IO;
using Google.Protobuf;
using log4net;
using System.Reflection;
using GameServer.Helper;
using GameServer.World.UserInfo;
using Messages;
using Library.Helper.Encrypt;
using System.Security.Cryptography;

namespace GameServer.Socket
{
    public class SessionActor : UntypedActor
    {
        // 성공 적으로 메시지 보내기 성공
        public class Ack : Tcp.Event 
        {
            public static Ack Instance { get; } = new();
        }

        public class UserToSessionLinkRequest
        {
            public IActorRef UserRef { get; set; } // 객체 연결
        }
        public class UserToSessionLinkResponse
        {
            public static UserToSessionLinkResponse Instance { get; } = new();
        }

        public class SendMessage
        {
            public MessageWrapper Message { get; set; } 
        }        

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IActorRef _sessionCordiatorRef;
        private readonly string _remoteAddress;
        private readonly IActorRef _connectedSessionRef; // 연결 된 원격지 클라이언트 session 
        
        private IActorRef _userRef;                     // 

        // TCP 특성상 다 오지 못 해, 버퍼가 쌓으면서 다 받으면 가져간다.
        private List<byte> _receivedBuffer = new List<byte>();
        private int? _totalReceivedMessageLength = null;
        private int? _messageLength = null;
        private const int _maxRecvLoop = 100; // 패킷받는 최대 카운트

        // TCP 패킷 보내는 것에 대한 처리
        private byte[] _sendingPacketBytes = null;
        private int _sendingBytes = 0; // 보내고 있는 패킷
        private int _sendedBytes = 0;  // 보낸 패킷
        private const int _maxPacketBytes = 1024;  // 최대 패킷 수
        private Queue<byte[]> _sendQueue = new Queue<byte[]>();

        // 패킷 암호화 여부
        private readonly bool _packetEncrypt = true;

        public SessionActor(IActorRef sessionCordiator, string remoteAdress, IActorRef connection)
        {
            _sessionCordiatorRef = sessionCordiator;
            _remoteAddress = remoteAdress;
            _connectedSessionRef = connection;
            _userRef = null;

            _packetEncrypt = ConfigInstanceHelper.Instance.PacketEncrypt;

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
            base.PostStop();
        }

        protected override void OnReceive(object message)
        {
            switch(message)
            {                
                case SessionActor.UserToSessionLinkRequest request:
                    {
                        _userRef = request.UserRef; // 네트워크 세션과 User Actor 연결

                        // 등록이 완료되었음
                        _userRef.Tell(SessionActor.UserToSessionLinkResponse.Instance);

                        
                        break;
                    }
                case Tcp.Received received: // 메시지 받기
                    {
                        _receivedBuffer.AddRange(received.Data.ToArray());

                        var intSize = sizeof(int);

                        // Loop while we might still have complete messages to process
                        for(var i = 0; i < _maxRecvLoop; ++i)
                        {
                            // If we don't know the length of the message yet (4 byte, int)
                            if (!_totalReceivedMessageLength.HasValue)
                            {
                                if (_receivedBuffer.Count < intSize)
                                    return;

                                _totalReceivedMessageLength = BitConverter.ToInt32(_receivedBuffer.ToArray(), 0);
                                _receivedBuffer.RemoveRange(0, intSize);
                            }

                            // decryption message size (4 byte, int)
                            if (!_messageLength.HasValue)
                            {
                                if (_receivedBuffer.Count < intSize)
                                    return;

                                _messageLength = BitConverter.ToInt32(_receivedBuffer.ToArray(), 0);
                                _receivedBuffer.RemoveRange(0, intSize);
                            }                            
                            // 메시지 크기
                            // 전체 패킷 사이즈 - decrpytionSize 사이즈
                            int encrypMessageSize = _totalReceivedMessageLength.Value - intSize;

                            // If entire message hasn't been received yet
                            if (_receivedBuffer.Count < encrypMessageSize)
                                return;

                            var messageSize = _messageLength.Value; // decrypt된 메시지 사이즈

                            // (암호화된)실제 메시지 읽기
                            var messageBytes = _receivedBuffer.GetRange(0, encrypMessageSize).ToArray();                                                        
                            _receivedBuffer.RemoveRange(0, encrypMessageSize);                            

                            // 초기화
                            _totalReceivedMessageLength = null;
                            _messageLength = null;

                            // 패킷 암호화 사용중이면 decryp해주자
                            byte[] receivedMessage = null;
                            if (_packetEncrypt)
                            {   
                                receivedMessage = CryptographyHelper.DecryptData(messageBytes, messageSize);
                            }
                            else
                            {
                                receivedMessage = messageBytes;
                            }

                            // Handle the message
                            _userRef?.Tell(new UserActor.SessionReceiveData
                            {
                                MessageSize = messageSize,
                                RecvBuffer = receivedMessage,
                            }, Self);
                        }

                        break;
                    }
                case SessionActor.SendMessage sendMessage:
                    {   
                        var requestBinary = sendMessage.Message.ToByteArray();
                        sendMessage.Message.MessageSize = requestBinary.Length;

                        int totalSize = sizeof(int); // messageSize만 넣고
                        int messageSize = requestBinary.Length;

                        byte[] binary = null;

                        if (_packetEncrypt)
                        {
                            binary = CryptographyHelper.EncryptData(requestBinary);
                            totalSize += binary.Length;
                        }
                        else
                        {
                            binary = requestBinary;
                            totalSize += requestBinary.Length;
                        }

                        byte[] byteArray = null;
                        using (var stream = new MemoryStream())
                        {
                            using (var writer = new BinaryWriter(stream))
                            {
                                writer.Write(totalSize); // size는 int으로
                                writer.Write(messageSize);
                                writer.Write(binary);
                                byteArray = stream.ToArray();
                            }
                        }
                        _sendQueue.Enqueue(byteArray);
                        SendPacket();

                        break;
                    }
                case Ack _:
                    {
                        // 패킷 전송 완료
                        SendPacket();
                        break;
                    }
                case SessionCordiatorActor.BroadcastMessage sendMessage:
                    {                        
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
                case Tcp.CommandFailed failed:
                    {
                        // 패킷 전송 실패 처리
                        _logger.Error($"Tcp.CommandFailed: {failed.Cmd}");

                        // 재시도를 위해 패킷을 다시 전송
                        SendPacket();
                        break;
                    }
                case Tcp.WritingResumed _:
                    {
                        // 패킷 전송 완료 처리
                        if (_sendedBytes >= _sendingBytes)
                        {
                            _logger.Debug("completed to send packet ");
                        }
                        else
                        {
                            // 전체 패킷이 전송되지 않았을 경우 재시도
                            SendPacket();
                        }
                        break;
                    }
                default:
                    {
                        _logger.Error($"{Sender} Unhandled");
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
            //_sessionCordiatorRef.Tell(new SessionCordiatorActor.ClosedRequest
            //{
            //    RemoteAdress = _remoteAddress
            //});
            Context.Stop(Self);
        }

        /// <summary>
        /// 현재 등록된 패킷을 보낸다.
        /// </summary>
        private void SendPacket()
        {
            if (_sendedBytes >= _sendingBytes)
            {
                // 다 보냈으면 다음 큐에서 가져온다.
                if (TrySendNextMessage() == false)
                {
                    // 큐에도 없으면 보낼것이 없는 것이다.
                    return;
                }                
            }

            // 남은 패킷 조각 계산
            int remainingBytes = _sendingBytes - _sendedBytes;

            // 전송할 패킷 조각 계산
            int chunkSize = Math.Min(remainingBytes, _maxPacketBytes); // 조각 크기는 적절히 조정 가능

            // 패킷 조각 생성
            var chunk = Akka.IO.ByteString.FromBytes(_sendingPacketBytes, _sendedBytes, chunkSize);

            // 패킷 조각 전송            
            if (chunk != null)
                _connectedSessionRef.Tell(Tcp.Write.Create(chunk, Ack.Instance));

            // 전송한 바이트 수 업데이트
            _sendedBytes += chunkSize;
        }

        /// <summary>
        /// 큐에서 다음 메시지를 가져와 보냅니다.
        /// </summary>
        private bool TrySendNextMessage()
        {
            // 큐에서 다음 메시지를 가져와 보냅니다.
            if (_sendQueue.Count <= 0)
                return false;

            var byteArray = _sendQueue.Dequeue();
            _sendingPacketBytes = byteArray;
            _sendingBytes = _sendingPacketBytes.Length;
            _sendedBytes = 0;

            return (_sendedBytes < _sendingBytes);                
        }
    }

}
