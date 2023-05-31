using Akka.Actor;
using Akka.IO;
using Google.Protobuf;
using Messages;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public class TelnetClient : UntypedActor
{
    private IActorRef _connection;
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
                HandleMyMessage(received);
            }
            else if (message is string s)   // data received from console
            {
                //var request = new SayRequest
                //{
                //    UserName = "test",
                //    Message = s
                //};
                var request = new MessageWrapper {
                    SayRequest = new SayRequest
                    {
                        Id = 1,
                        User = "test",
                        Message = s
                    }
                };

                var binary = request.ToByteArray();
                connection.Tell(Tcp.Write.Create(Akka.IO.ByteString.FromBytes(binary)));

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
    private bool HandleMyMessage(Tcp.Received received)
    {
        var receivedMessage = received.Data.ToArray();

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