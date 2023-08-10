using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.IO;
using Google.Protobuf;
using log4net;
using Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LobbyServer.Helper;
using Akka.Dispatch.SysMsg;
using LobbyServer.World.UserInfo;

namespace LobbyServer.Socket
{
    public class SessionCordiatorActor : UntypedActor
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

        public class SetUserCordiatorActor
        {
            public IActorRef UserCordiatorActor { get; set; }

        }
        public class BroadcastMessage
        {
            public MessageWrapper Message { get; set; }
        }

        private readonly ConcurrentDictionary<string, IActorRef> _sessions = new ();
        private readonly ConcurrentDictionary<IActorRef, string> _sessionRefs = new ();

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
        }
        protected override void PostStop()
        {
            _sessions.Clear();
            _sessionRefs.Clear();

            base.PostStop();
        }

        // here we are overriding the default SupervisorStrategy
        // which is a One-For-One strategy w/ a Restart directive
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, // maxNumberOfRetries
                TimeSpan.FromSeconds(30), // duration
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
        protected override void OnReceive(object message)
        {
            switch(message)
            {
                case SessionCordiatorActor.RegisteredRequest registeredRequest:
                    {
                        OnReceiveRegister(registeredRequest);
                        break;
                    }
                case SessionCordiatorActor.ClosedRequest closedRequest:
                    {
                        var remoteAddress = closedRequest.RemoteAdress;
                        if (_sessions.TryGetValue(remoteAddress, out var session))
                        {
                            Context.Unwatch(session);
                            Context.Stop(session);

                            _sessionRefs.TryRemove(session, out var _);
                            // remove the actor reference from the dictionary
                            _sessions.TryRemove(remoteAddress, out _);
                        }
                        
                        var userCordiatorRef = Context.ActorSelection(ActorPaths.UserCordiator.Path);
                        userCordiatorRef.Tell(new UserCordiatorActor.ClosedUserSession
                        {
                            RemoteAddress = remoteAddress
                        });

                        break;
                    }
                case SessionCordiatorActor.BroadcastMessage broadcastMessage:
                    {
                        var binary = broadcastMessage.Message.ToByteArray();
                        var bytes = Tcp.Write.Create(Akka.IO.ByteString.FromBytes(binary));

                        var sendMessage = new SessionActor.SendMessage
                        {
                            Message = broadcastMessage.Message
                        };

                        foreach (var sessionRef in _sessions.Values)
                        {
                            sessionRef.Tell(sendMessage);
                        }
                        break;
                    }
                case Tcp.WritingResumed writingResumed:
                    {
                        break;
                    }
                case Terminated terminated:
                    {
                        // Here, handle the termination of the watched actor.
                        // For example, you might want to create a new actor or simply log the termination.
                        if(_sessionRefs.TryGetValue(terminated.ActorRef, out var remoteAdress))
                        {
                            _logger.Info($"client disconnected:{remoteAdress}");

                            if (_sessions.TryGetValue(remoteAdress, out var session))
                            {
                                Context.Unwatch(session);                                

                                _sessionRefs.TryRemove(session, out var _);
                                // remove the actor reference from the dictionary
                                _sessions.TryRemove(remoteAdress, out _);
                            }
                            

                            var userCordiatorRef = Context.ActorSelection(ActorPaths.UserCordiator.Path); 
                            userCordiatorRef.Tell(new UserCordiatorActor.ClosedUserSession
                            {
                                RemoteAddress = remoteAdress
                            });
                        }
                        break;
                    }
                default:
                    {
                        Unhandled(message);
                        break;
                    }
            }
        }

        /// <summary>
        /// 원격 세션 추가
        /// </summary>
        /// <param name="message"></param>
        private void OnReceiveRegister(SessionCordiatorActor.RegisteredRequest message)
        {
            // create a new session actor
            var remoteSender = message.Sender;

            var sessionProp = Props.Create(() => new SessionActor(Self, message.RemoteAdress, remoteSender));
            var sessionRef = Context.ActorOf(sessionProp);
            remoteSender.Tell(new Tcp.Register(sessionRef));

            // 자식 Session이 PostStop일때 Terminated 이벤트를 받을 수 있다.
            Context.Watch(sessionRef);

            // session관리에 넣어주자
            _sessions.TryAdd(message.RemoteAdress, sessionRef);
            _sessionRefs.TryAdd(sessionRef, message.RemoteAdress);

            // 월드에 추가해 주자            
            var userCordiatorRef = Context.ActorSelection(ActorPaths.UserCordiator.Path);
            userCordiatorRef.Tell(new UserCordiatorActor.AddUser
            {
                SessionRef = sessionRef,
                RemoteAddress = message.RemoteAdress
            });
        }
    }
}
