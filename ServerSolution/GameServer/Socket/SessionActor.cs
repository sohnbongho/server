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
using System.Buffers;
using Library.Memory;
using System.Collections;
using System.IO.Pipelines;
using static Akka.IO.Tcp;
using System.Reflection.PortableExecutable;
using static Akka.IO.Udp;
using System.Drawing;

namespace GameServer.Socket
{    
    public class Packet
    {
        public const int MaxBufferBytes = 4096;  // 최대 패킷 수
        public const ushort UShortSize = sizeof(ushort);
        public ushort Length { get; set; } = 0;
        public Byte[] Buffer = new Byte[MaxBufferBytes];
        public void Init()
        {
            Length = 0;            
        }
    }
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
        private readonly IActorRef _connectedSocket; // 연결 된 원격지 클라이언트 session 
        
        private IActorRef _userRef;                     // 

        // 패킷 읽는 부분
        private List<byte> _receivedBuffer = new List<byte>();
        private ushort? _totalReceivedMessageLength = null;
        private ushort? _messageLength = null;
        private const int _maxRecvLoop = 100; // 패킷받는 최대 카운트
        private Pipe _pipe = new Pipe();

        // TCP 패킷 보내는 것에 대한 처리
        private Packet _sendingPacket = null;
        private byte[] _sendingPacketBytes = null;
        private int _sendingBytes = 0; // 보내고 있는 패킷
        private int _sendedBytes = 0;  // 보낸 패킷        
        private Queue<Packet> _sendQueue = new Queue<Packet>();
        // 메모리 풀
        private PacketMemoryPool<Packet> _sendMemoryPool = new PacketMemoryPool<Packet>();

        // 패킷 암호화 여부
        private readonly bool _packetEncrypt = true;

        // MemoryStream방식
        private MemoryStream _receivedStream = new MemoryStream();        
        private byte[] _remainingData = null;
        private byte[] _lengthBytes = new byte[Packet.UShortSize];
        private long _remainingDataSize = Packet.MaxBufferBytes;


        public SessionActor(IActorRef sessionCordiator, string remoteAdress, IActorRef connection)
        {
            _sessionCordiatorRef = sessionCordiator;
            _remoteAddress = remoteAdress;
            _connectedSocket = connection;
            _userRef = null;
            _remainingData = new byte[_remainingDataSize];

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
            _logger.Debug("SessionActor.PostStop");
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
                        //HandleReceivedAsync(received.Data.ToArray());
                        HandleReceived(received.Data.ToArray());
                        break;
                    }
                    
                case SessionActor.SendMessage sendMessage:
                    {   
                        var requestBinary = sendMessage.Message.ToByteArray();
                        sendMessage.Message.MessageSize = requestBinary.Length;

                        ushort totalSize = sizeof(ushort); // messageSize만 넣고
                        ushort messageSize = (ushort)requestBinary.Length;

                        byte[] binary = null;

                        if (_packetEncrypt)
                        {
                            binary = CryptographyHelper.EncryptData(requestBinary);
                            totalSize += (ushort)binary.Length;
                        }
                        else
                        {
                            binary = requestBinary;
                            totalSize += (ushort)requestBinary.Length;
                        }

                        //{
                        //    byte[] byteArray = null;
                        //    using (var stream = new MemoryStream())
                        //    {
                        //        using (var writer = new BinaryWriter(stream))
                        //        {
                        //            writer.Write(totalSize); // size는 int으로
                        //            writer.Write(messageSize);
                        //            writer.Write(binary);
                        //            byteArray = stream.ToArray();
                        //        }
                        //    }
                        //}                        
                        {
                            // 메모리 풀에서 필요한 만큼의 메모리를 대여합니다.
                            Packet packet = _sendMemoryPool.Rent();

                            byte[] totalSizeBytes = BitConverter.GetBytes(totalSize);
                            byte[] messageSizeBytes = BitConverter.GetBytes(messageSize);

                            // binary가 이미 byte[]인 것으로 가정
                            packet.Length = (ushort)(totalSizeBytes.Length + messageSizeBytes.Length + binary.Length);                            

                            // 대여한 메모리에 복사
                            Buffer.BlockCopy(totalSizeBytes, 0, packet.Buffer, 0, totalSizeBytes.Length);
                            Buffer.BlockCopy(messageSizeBytes, 0, packet.Buffer, totalSizeBytes.Length, messageSizeBytes.Length);
                            Buffer.BlockCopy(binary, 0, packet.Buffer, totalSizeBytes.Length + messageSizeBytes.Length, binary.Length);

                            _sendQueue.Enqueue(packet);
                        }

                        
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
        private async Task HandleReceivedAsync(byte[] data)
        {
            // 비동기 작업 코드
            // 받은 데이터를 파이프에 씁니다.            
            // PipeWriter의 버퍼에 데이터를 씁니다.
            await _pipe.Writer.WriteAsync(data);            

            var intSize = sizeof(ushort);

            // Loop while we might still have complete messages to process
            for (var i = 0; i < _maxRecvLoop; ++i)
            {
                // PipeReader를 사용하여 읽기 작업을 수행합니다.
                // TryRead를 사용하면 즉시 결과를 반환하므로 대기하지 않습니다.
                ReadResult result = await _pipe.Reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                // If we don't know the length of the message yet (2 bytes, ushort)
                if (!_totalReceivedMessageLength.HasValue)
                {
                    if (buffer.Length < intSize)
                    {
                        // 읽은 데이터를 소비합니다.                        
                        return;
                    }

                    _totalReceivedMessageLength = BitConverter.ToUInt16(buffer.FirstSpan);
                    buffer = buffer.Slice(intSize);
                }

                // Decryption message size (2 bytes, ushort)
                if (!_messageLength.HasValue)
                {
                    if (buffer.Length < intSize)
                    {
                        // 읽은 데이터를 소비합니다.
                        return;
                    }

                    _messageLength = BitConverter.ToUInt16(buffer.FirstSpan);
                    buffer = buffer.Slice(intSize);
                }

                // 메시지 크기
                // 전체 패킷 사이즈 - decrpytionSize 사이즈
                int encrypMessageSize = _totalReceivedMessageLength.Value - intSize;

                // If entire message hasn't been received yet
                if (buffer.Length < encrypMessageSize)
                {
                    // 읽은 데이터를 소비합니다.                    
                    return;
                }

                var messageSize = _messageLength.Value; // decrypt된 메시지 사이즈

                // (암호화된)실제 메시지 읽기
                var messageBytes = buffer.Slice(0, encrypMessageSize).ToArray();
                buffer = buffer.Slice(encrypMessageSize);

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

                // 메시지를 처리한 후에는 AdvanceTo 메서드를 호출하여 파이프 리더가 처리한 데이터를 소비하도록 합니다.
                _pipe.Reader.AdvanceTo(buffer.Start);
            }            
        }

        private void HandleReceived(byte[] data)
        {
            // 받은 데이터를 메모리 스트림에 씁니다.
            _receivedStream.Write(data, 0, data.Length);
            _receivedStream.Position = 0;

            // Loop while we might still have complete messages to process
            for (var i = 0; i < _maxRecvLoop; ++i)
            {
                // If we don't know the length of the message yet (2 bytes, ushort)
                if (!_totalReceivedMessageLength.HasValue)
                {
                    if (_receivedStream.Length - _receivedStream.Position < sizeof(ushort))
                        return;
                                        
                    _receivedStream.Read(_lengthBytes, 0, sizeof(ushort));
                    _totalReceivedMessageLength = BitConverter.ToUInt16(_lengthBytes, 0);
                }

                // Decryption message size (2 bytes, ushort)
                if (!_messageLength.HasValue)
                {
                    if (_receivedStream.Length - _receivedStream.Position < sizeof(ushort))
                        return;
                                        
                    _receivedStream.Read(_lengthBytes, 0, sizeof(ushort));
                    _messageLength = BitConverter.ToUInt16(_lengthBytes, 0);
                }

                int encrypMessageSize = _totalReceivedMessageLength.Value - sizeof(ushort);

                // If entire message hasn't been received yet
                if (_receivedStream.Length - _receivedStream.Position < encrypMessageSize)
                    return;

                var messageSize = _messageLength.Value;
                byte[] messageBytes = new byte[encrypMessageSize];
                _receivedStream.Read(messageBytes, 0, encrypMessageSize);

                // 초기화
                _totalReceivedMessageLength = null;
                _messageLength = null;

                // 패킷 암호화 unpack
                byte[] receivedMessage = _packetEncrypt
                    ? CryptographyHelper.DecryptData(messageBytes, messageSize)
                    : messageBytes;

                // Handle the message
                _userRef?.Tell(new UserActor.SessionReceiveData
                {
                    MessageSize = messageSize,
                    RecvBuffer = receivedMessage,
                }, Self);

                // 데이터가 남았는가?
                long bytesToKeep = _receivedStream.Length - _receivedStream.Position;
                if(bytesToKeep > 0)
                {                    
                    if(bytesToKeep > _remainingDataSize)
                    {
                        _remainingDataSize = bytesToKeep;
                        _remainingData = new byte[_remainingDataSize];
                    }
                    
                    _receivedStream.Read(_remainingData, 0, (int)bytesToKeep);

                    _receivedStream.SetLength(bytesToKeep);
                    _receivedStream.Position = 0;
                    _receivedStream.Write(_remainingData, 0, (int)bytesToKeep);
                }
                else
                {
                    // 모든 데이터 읽기 성공
                    _receivedStream.SetLength(0);
                    _receivedStream.Position = 0;
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
            int chunkSize = Math.Min(remainingBytes, Packet.MaxBufferBytes); // 조각 크기는 적절히 조정 가능

            // 패킷 조각 생성
            var chunk = Akka.IO.ByteString.FromBytes(_sendingPacketBytes, _sendedBytes, chunkSize);

            // 패킷 조각 전송            
            if (chunk != null)
                _connectedSocket.Tell(Tcp.Write.Create(chunk, Ack.Instance));

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

            if(_sendingPacket != null)
            {
                // 전에 메모리 풀에서 가져온 버퍼 반환
                _sendMemoryPool.Return(_sendingPacket);
            }

            _sendingPacket = _sendQueue.Dequeue();
            
            _sendingPacketBytes = _sendingPacket.Buffer; 
            _sendingBytes = _sendingPacket.Length;
            _sendedBytes = 0;

            return (_sendedBytes < _sendingBytes);                
        }
    }

}
