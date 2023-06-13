using Akka.Actor;
using GameServer.Helper;
using GameServer.Socket;
using Google.Protobuf.WellKnownTypes;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static GameServer.World.UserInfo.UserCordiatorActor;

namespace GameServer.World.UserInfo
{
    public class UserCordiatorActor : UntypedActor
    {
        public class AddUser
        {
            public IActorRef SessionRef { get; set; }
            public string RemoteAddress { get; set; }
        }
        public class ClosedUserSession
        {
            public string RemoteAddress { get; set; }
        }

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IActorRef _worldRef;
        // 원격에 연결된 User Acotr들
        private readonly ConcurrentDictionary<string, User> _userList = new ();
        private readonly ConcurrentDictionary<IActorRef, string> _userRefs = new();

        public static IActorRef ActorOf(IUntypedActorContext context, IActorRef worldRef)
        {
            var prop = Props.Create(() => new UserCordiatorActor(worldRef));
            return context.ActorOf(prop, ActorPaths.UserCordiator.Name);
        }

        public UserCordiatorActor(IActorRef worldRef)
        {
            _worldRef = worldRef;
        }
        protected override void PostStop()
        {
            _userList.Clear();
            _userRefs.Clear();

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
                });
        }

        protected override void OnReceive(object message)
        {
            switch(message)
            {
                case UserCordiatorActor.AddUser addUser:
                    {
                        var remoteAddress = addUser.RemoteAddress;
                        _logger.Debug($"OnRecvAddUser RemoteAddress:{remoteAddress}, SessionRef:{addUser.SessionRef}");

                        if(_userList.TryGetValue(remoteAddress, out var _) == false)
                        {
                            var user = User.Of(Context, Self, addUser.SessionRef, remoteAddress);

                            // 자식 UserActor가 PostStop일때 Terminated 이벤트를 받을 수 있다.
                            Context.Watch(user.UserRef);

                            // 유저 추가
                            _userList.TryAdd(remoteAddress, user);
                            _userRefs.TryAdd(user.UserRef, remoteAddress);
                        }
                        else
                        {
                            // 유저 등록 실패면 session을 닫는다.
                            _logger.Error($"fail to OnRecvAddUser RemoteAddress:{remoteAddress}, SessionRef:{addUser.SessionRef}");
                            
                            var sessionCordiatorRef = Context.ActorSelection(ActorPaths.SessionCordiator.Path); ;
                            sessionCordiatorRef.Tell(new SessionCordiatorActor.ClosedRequest
                            {
                                RemoteAdress = remoteAddress
                            });
                        }

                        break;
                    }
                case UserCordiatorActor.ClosedUserSession closedUserSession:
                    {
                        var remoteAdress = closedUserSession.RemoteAddress;
                        if (_userList.TryGetValue(remoteAdress, out var finedUser))
                        {
                            _userRefs.TryRemove(finedUser.UserRef, out _);                           

                            // remove the actor reference from the dictionary
                            _userList.TryRemove(closedUserSession.RemoteAddress, out var _);

                            Context.Unwatch(finedUser.UserRef);
                            Context.Stop(finedUser.UserRef);                            
                        }
                        break;
                    }
                case Terminated terminated:
                    {
                        // Here, handle the termination of the watched actor.
                        // For example, you might want to create a new actor or simply log the termination.
                        if (_userRefs.TryGetValue(terminated.ActorRef, out var remoteAddress))
                        {
                            Context.Unwatch(terminated.ActorRef);

                            _userRefs.TryRemove(terminated.ActorRef, out _);
                            _userList.TryRemove(remoteAddress, out var _);

                            var sessionCordiatorRef = Context.ActorSelection(ActorPaths.SessionCordiator.Path); ;
                            sessionCordiatorRef.Tell(new SessionCordiatorActor.ClosedRequest
                            {
                                RemoteAdress = remoteAddress
                            });
                        }
                        break;
                    }
                default: 
                    {
                        var senderPath = Sender.Path.ToString();
                        if (_userList.TryGetValue(senderPath, out var findedUser))
                        {
                            findedUser.UserRef.Tell(message);
                        }
                        break;
                    }
            }
        }
    }
}
