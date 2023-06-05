using Akka.Actor;
using Akka.IO;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TestLibrary.Helper.Encrypt;
using TestLibrary.Messages;

public class TelnetClient : UntypedActor
{
    private IActorRef _connection;

    // TCP 특성상 다 오지 못 해, 버퍼가 쌓으면서 다 받으면 가져간다.
    private List<byte> _receivedBuffer = new List<byte>();
    private int? _totalReceivedMessageLength = null;
    private int? _messageLength = null;
    private const int _maxRecvLoop = 100; // 패킷받는 최대 카운트

    private string  _userId = string.Empty; // 패킷받는 최대 카운트
    private bool _connected = false;
    private string _testSessionKey = "1234567";

    private bool _encrypt = true; // 암호화 사용


    public TelnetClient(string host, int port)
    {
        var endpoint = new DnsEndPoint(host, port);
        Context.System.Tcp().Tell(new Tcp.Connect(endpoint));
    }

    protected override void OnReceive(object message)
    {
        if (message is Tcp.Connected connected)
        {
            Console.WriteLine("Connected to {0}", connected.RemoteAddress);

            // Register self as connection handler
            Sender.Tell(new Tcp.Register(Self));
            ReadConsoleAsync();
            Become(Connected(Sender));

            // 서버에 입장
            var request = new MessageWrapper{
                ServerEnterRequest = new ServerEnterRequest{
                    SessionKey = _testSessionKey                    
                }
            };
            Tell(request);

        }
        else if (message is Tcp.CommandFailed)
        {
            Console.WriteLine("Connection failed");
        }
        else if (message is string msg)
        {
            _connection.Tell(Tcp.Write.Create(Akka.IO.ByteString.FromString(msg + "\n")));
        }
        else
        {
            Unhandled(message);
        }
    }

    private UntypedReceive Connected(IActorRef connection)
    {
        _connection = connection;
        return message =>
        {
            if (message is Tcp.Received received)  // data received from network
            {
                //Console.WriteLine(Encoding.ASCII.GetString(received.Data.ToArray()));                

                _receivedBuffer.AddRange(received.Data.ToArray());

                var intSize = sizeof(int);

                // Loop while we might still have complete messages to process
                for (var i = 0; i < _maxRecvLoop; ++i)
                {
                    // If we don't know the length of the message yet (4 byte, int)
                    if (!_totalReceivedMessageLength.HasValue)
                    {
                        if (_receivedBuffer.Count < sizeof(int))
                            return;

                        _totalReceivedMessageLength = BitConverter.ToInt32(_receivedBuffer.ToArray(), 0);
                        _receivedBuffer.RemoveRange(0, sizeof(int));
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
                    if (_encrypt)
                    {
                        receivedMessage = CryptographyHelper.DecryptData(messageBytes, messageSize);
                    }
                    else
                    {
                        receivedMessage = messageBytes;
                    }

                    // Handle the message
                    HandleMyMessage(receivedMessage);


                }
            }
            else if (message is string s)   // data received from console
            {
                //var request = new SayRequest
                //{
                //    UserName = "test",
                //    Message = s
                //};                
                if(!_connected)
                {
                    ReadConsoleAsync();
                    return;
                }
                var request = new MessageWrapper
                {
                    SayRequest = new SayRequest
                    {
                        UserId = _userId,
                        Message = s
                    }
                };
                Tell(request);
                ReadConsoleAsync();
            }
            else if (message is Tcp.PeerClosed)
            {
                Console.WriteLine("Connection closed");
            }
            else
            {
                Unhandled(message);
            }
        };
    }

    private void ReadConsoleAsync()
    {
        Task.Factory.StartNew(self => Console.In.ReadLineAsync().PipeTo((ICanTell)self), Self);
    }
    private void Tell(MessageWrapper request)
    {
        var requestBinary = request.ToByteArray();
        request.MessageSize = requestBinary.Length;

        int totalSize = sizeof(int);
        int messageSize = requestBinary.Length;

        byte[] binary = null;
        
        if (_encrypt)
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
        if (byteArray != null)
            _connection.Tell(Tcp.Write.Create(Akka.IO.ByteString.FromBytes(byteArray)));

    }
    private bool HandleMyMessage(byte[] recvBuffer)
    {
        var receivedMessage = recvBuffer;

        // 전체를 관리하는 wapper로 변환 역직렬화
        var wrapper = MessageWrapper.Parser.ParseFrom(receivedMessage);
        Console.WriteLine($"OnRecvPacket {wrapper.PayloadCase.ToString()}");
        switch (wrapper.PayloadCase)
        {
            case MessageWrapper.PayloadOneofCase.ServerEnterResponse:
                {
                    var response = wrapper.ServerEnterResponse;

                    _userId = response.UserId;
                    _connected = true;
                    break;
                }
            case MessageWrapper.PayloadOneofCase.SayResponse:
                {
                    var response = wrapper.SayResponse;
                    Console.WriteLine($"{response.UserId} : {response.Message}");

                    break;
                }
        }
        return true;
    }
}