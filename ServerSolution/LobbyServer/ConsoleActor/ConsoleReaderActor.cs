
using System;
using LobbyServer.Helper;
using Akka.Actor;
using Akka.IO;

namespace LobbyServer.ConsoleActor
{
    /// <summary>
    /// Actor responsible for reading FROM the console.
    /// Also responsible for calling <see cref="ActorSystem.Terminate"/>.
    /// </summary>
    class ConsoleReaderActor : ReceiveActor
    {
        public const string StartCommand = "start";
        public const string ExitCommand = "exit";
        public const string UpdateCommand = "update";
        private readonly IActorRef _consoleWriterActor;

        public static IActorRef ActorOf(ActorSystem actorSystem, IActorRef consoleWriterActor)
        {
            var consoleReaderProps = Props.Create(() => new ConsoleReaderActor(consoleWriterActor));
            return actorSystem.ActorOf(consoleReaderProps, ActorPaths.ReaderConsole.Name);
        }

        public ConsoleReaderActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;

            Receive<string>(
              message =>
              {
                  GetAndValidateInput();
              }
          );
            
        }

        /// <summary>
        /// Reads input from console, validates it, then signals appropriate response
        /// (continue processing, error, success, etc.).
        /// </summary>
        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();
            if (!string.IsNullOrEmpty(message) && String.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // if user typed ExitCommand, shut down the entire actor system (allows the process to exit)
                Context.System.Terminate();
                return;
            }

            // otherwise, just send the message off for validation
            // 커맨드를 날려준다.            
            _consoleWriterActor.Tell(message);
        }        
    }
}
