using Akka.Actor;
using Akka.IO;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

public class TelnetClient : UntypedActor
{
    private IActorRef _connection;

    // TCP 특성상 다 오지 못 해, 버퍼가 쌓으면서 다 받으면 가져간다.
    private List<byte> _buffer = new List<byte>();
    private int? _currentMessageLength = null;
    private const int _maxRecvLoop = 100; // 패킷받는 최대 카운트

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
        }
        else if (message is Tcp.CommandFailed)
        {
            Console.WriteLine("Connection failed");
        }
        else if(message is string msg)
        {
            _connection.Tell(Tcp.Write.Create(Akka.IO.ByteString.FromString(msg + "\n")));
        }
        else Unhandled(message);
    }

    private UntypedReceive Connected(IActorRef connection)
    {
        _connection = connection;
        return message =>
        {
            if (message is Tcp.Received received)  // data received from network
            {
                //Console.WriteLine(Encoding.ASCII.GetString(received.Data.ToArray()));                

                _buffer.AddRange(received.Data.ToArray());

                // Loop while we might still have complete messages to process
                for (var i = 0; i < _maxRecvLoop; ++i)
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

                    //var message = Encoding.UTF8.GetString(messageBytes);
                    _buffer.RemoveRange(0, _currentMessageLength.Value);
                    _currentMessageLength = null;

                    // Handle the message
                    HandleMyMessage(messageBytes);                    
                }
            }
            else if (message is string s)   // data received from console
            {
                //var request = new SayRequest
                //{
                //    UserName = "test",
                //    Message = s
                //};                

                var sayRequest = new SayRequest
                {
                    Id = 1,
                    User = "test",
                    Message = s
                };                

                var request = new MessageWrapper {                    
                    SayRequest = sayRequest
                };
                var binary = request.ToByteArray();
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
                if (byteArray != null)
                    connection.Tell(Tcp.Write.Create(Akka.IO.ByteString.FromBytes(byteArray)));

                ReadConsoleAsync();
            }
            else if (message is Tcp.PeerClosed)
            {
                Console.WriteLine("Connection closed");
            }
            else Unhandled(message);
        };
    }

    private void ReadConsoleAsync()
    {
        Task.Factory.StartNew(self => Console.In.ReadLineAsync().PipeTo((ICanTell)self), Self);
    }
    private bool HandleMyMessage(byte[] recvBuffer)
    {
        var receivedMessage = recvBuffer;

        // 전체를 관리하는 wapper로 변환 역직렬화
        var wrapper = MessageWrapper.Parser.ParseFrom(receivedMessage);
        Console.WriteLine($"OnRecvPacket {wrapper.PayloadCase.ToString()}");
        switch (wrapper.PayloadCase)
        {
            case MessageWrapper.PayloadOneofCase.SayResponse:
                {
                    var response = wrapper.SayResponse;
                    Console.WriteLine($"{response.User} : {response.Message}");

                    break;
                }
        }
        return true;
    }
}