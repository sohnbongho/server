﻿using System;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using LoginServer.Helper;

namespace LoginServer.ConsoleActor
{
    /// <summary>
    /// Actor responsible for serializing message writes to the console.
    /// (write one message at a time, champ :)
    /// </summary>
    class ConsoleWriterActor : ReceiveActor
    {
        public static IActorRef ActorOf(ActorSystem actorSystem)
        {
            var consoleWriterProps = Props.Create(() => new ConsoleWriterActor());
            return actorSystem.ActorOf(consoleWriterProps, ActorPaths.WriterConsole.Name);
        }
        public ConsoleWriterActor()
        {
            Receive<string>(
               message =>
               {
                   Console.ForegroundColor = ConsoleColor.Red;
                   Console.WriteLine($"server message: {message}");
                   Context.ActorSelection(ActorPaths.ReaderConsole.Path).Tell(ConsoleReaderActor.UpdateCommand);
                   Console.ResetColor();
               }
           );         
        }        
    }
}
