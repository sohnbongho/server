﻿using Akka.Actor;
using Akka.IO;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestLibrary;
using TestServer.Helper;
using TestServer.World;
using static TestServer.Socket.SessionActor;

namespace TestServer.Socket
{
    public class SessionCordiatorActor : ReceiveActor
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public class RegisteredRequest
        {
            public string RemoteAdress { get; set; }
            public IActorRef Sender { get; set; }
        }

        public class ClosedRequest
        {
            public string RemoteAdress { get; set; }

        }
        public class BroadcastMessage
        {
            public GenericMessage Message { get; set; }
        }

        private readonly ConcurrentDictionary<string, IActorRef> _sessions = new ConcurrentDictionary<string, IActorRef>();
        private readonly IActorRef _listenerRef;
        private readonly IActorRef _worldRef;

        public static IActorRef ActorOf(IUntypedActorContext context, IActorRef listenerRef, IActorRef worldRef)
        {
            var prop = Props.Create(() => new SessionCordiatorActor(listenerRef, worldRef));
            return context.ActorOf(prop, ActorPaths.SessionCordiator.Name);
        }

        public SessionCordiatorActor(IActorRef listenerActor, IActorRef worldRef)
        {
            _listenerRef = listenerActor;
            _worldRef = worldRef;

            Receive<SessionCordiatorActor.RegisteredRequest>(message =>
            {
                OnReceiveCreate(message);
            });

            Receive<SessionCordiatorActor.ClosedRequest>(message =>
            {
                OnReceiveClosedSocket(message);
            });
            
            Receive<SessionCordiatorActor.BroadcastMessage>(message =>
            {                
                var binary = message.Message.ToByteArray();
                var bytes = Tcp.Write.Create(ByteString.FromBytes(binary));
                var sendMessage = new SessionActor.SendMessage
                {
                    Message = message.Message

                };
                foreach (var sessionRef in _sessions.Values)
                {
                    sessionRef.Tell(sendMessage);
                }
            });
        }
        protected override void PreStart()
        {


        }

        protected override void PostStop()
        {

        }
        // here we are overriding the default SupervisorStrategy
        // which is a One-For-One strategy w/ a Restart directive
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, // maxNumberOfRetries
                TimeSpan.FromSeconds(5), // duration
                x =>
                {
                    return Directive.Restart;

                    ////Maybe we consider ArithmeticException to not be application critical
                    ////so we just ignore the error and keep going.
                    //if (x is ArithmeticException) return Directive.Resume;

                    ////Error that we cannot recover from, stop the failing actor
                    //else if (x is NotSupportedException) return Directive.Stop;

                    ////In all other cases, just restart the failing actor
                    //else return Directive.Restart;
                });
        }

        /// <summary>
        /// 원격 세션 추가
        /// </summary>
        /// <param name="message"></param>
        private void OnReceiveCreate(SessionCordiatorActor.RegisteredRequest message)
        {
            // create a new session actor
            var remoteSender = message.Sender;

            var sessionRef = Context.ActorOf(Props.Create(() => new SessionActor(Self, message.RemoteAdress, remoteSender)));
            remoteSender.Tell(new Tcp.Register(sessionRef));

            // session관리에 넣어주자
            _sessions.TryAdd(message.RemoteAdress, sessionRef);

            // 월드에 추가해 주자
            _worldRef.Tell(new WorldActor.AddUser
            {
                SessionRef = sessionRef,
                RemoteAddress = message.RemoteAdress
            });
        }

        private void OnReceiveClosedSocket(SessionCordiatorActor.ClosedRequest message)
        {
            var remoteAdress = message.RemoteAdress;
            if (_sessions.TryGetValue(remoteAdress, out var session))
            {
                Context.Stop(session);

                // remove the actor reference from the dictionary
                _sessions.TryRemove(remoteAdress, out _);
            }

            _worldRef.Tell(new WorldActor.DeleteUser
            {
                RemoteAddress = remoteAdress
            });
        }
        
    }
}
