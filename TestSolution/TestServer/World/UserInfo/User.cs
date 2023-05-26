using Akka.Actor;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestServer.Helper;

namespace TestServer.World.UserInfo
{
    public class User 
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IActorRef WorldRef{ get; private set; } // 나를 포함하고 있는 월드
        public IActorRef SessionRef { get; private set; } // 원격지 Actor
        public IActorRef UserRef { get; private set; } // 내가 속해 있는 유저

        public static User Of(IUntypedActorContext context, IActorRef worldActor, IActorRef remoteRef)
        {
            var props = Props.Create(() => new UserActor(worldActor, remoteRef));
            var userActor = context.ActorOf(props, ActorPaths.User.Name);

            return new User(worldActor, remoteRef, userActor);
        }

        public User(IActorRef worldActor, IActorRef remoteRef, IActorRef userActor)
        {
            WorldRef = worldActor;
            SessionRef = remoteRef;
            UserRef = userActor;
        }
    }
    public class UserActor : ReceiveActor, ILogReceive
    {
        public IActorRef WorldActor;
        public IActorRef RemoteRef; // 원격지 Actor

        public UserActor(IActorRef worldActor, IActorRef remoteRef)
        {
            WorldActor = worldActor;
            RemoteRef = remoteRef;

        }

    }
}
