using Akka.Actor;
using Akka.IO;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TestLibrary;

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
            _connection.Tell(Tcp.Write.Create(ByteString.FromString(msg + "\n")));
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
                var request = new SayRequest
                {
                    UserName = "test",
                    Message = s
                };
                var binary = request.ToByteArray();
                connection.Tell(Tcp.Write.Create(ByteString.FromBytes(binary)));
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
        var messageObject = GenericMessage.FromByteArray(received.Data.ToArray());

        switch (messageObject)
        {
            case SayResponse sayResponse:
                {
                    Console.WriteLine($"{sayResponse.UserName} : {sayResponse.Message}");

                    break;
                }
        }
        return true;
    }
}