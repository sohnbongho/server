using Akka.Actor;
using Akka.IO;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestServer.Helper;

namespace TestServer.Socket
{
    public class SessionCordiatorActor : ReceiveActor
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);        

        public class Create
        {
            public string RemoteAdress { get; set; }
            public IActorRef Sender { get; set; }
        }

        public class Delete
        {
            public string RemoteAdress { get; set; }
            
        }

        private readonly ConcurrentDictionary<string, IActorRef> _sessions = new ConcurrentDictionary<string, IActorRef>();
        private readonly IActorRef _listenerActor;

        public static IActorRef ActorOf(IUntypedActorContext context, IActorRef listenerRef)
        {
            var prop = Props.Create(() => new SessionCordiatorActor(listenerRef));
            return context.ActorOf(prop, ActorPaths.SessionCordiator.Name);
        }

        public SessionCordiatorActor(IActorRef listenerActor)
        {
            _listenerActor = listenerActor;

            Receive<Create>(message =>
            {
                OnReceiveCreate(message);                
            });

            Receive<Delete>(message =>
            {
                if(_sessions.TryGetValue(message.RemoteAdress, out var session))
                {
                    Context.Stop(session);
                    // remove the actor reference from the dictionary
                    _sessions.TryRemove(message.RemoteAdress, out _);
                }
                
            });
        }
        protected override void PreStart()
        {
            

        }

        protected override void PostStop()
        {
            
        }

        /// <summary>
        /// 원격 세션 추가
        /// </summary>
        /// <param name="message"></param>
        private void OnReceiveCreate(Create message)
        {
            // create a new session actor
            var remoteSender = message.Sender;

            var session = Context.ActorOf(Props.Create(() => new SessionActor(Self, message.RemoteAdress, remoteSender)));
            remoteSender.Tell(new Tcp.Register(session));

            // session관리에 넣어주자
            _sessions.TryAdd(message.RemoteAdress, session);

            // 월드에 추가해 주자
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
    }
}
