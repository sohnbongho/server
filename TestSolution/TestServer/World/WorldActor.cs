using System;
using Akka.Actor;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TestLibrary;
using System.Collections.Generic;
using System.Threading;
using log4net;
using System.Reflection;
using Akka.IO;
using Akka.Remote;
using Akka.Event;
using TestServer.Helper;
using TestServer.World.UserInfo;
using static TestServer.World.WorldActor;

namespace TestServer.World
{
    /// <summary>
    /// 채팅 서버 액터
    /// </summary>
    public class WorldActor : ReceiveActor, ILogReceive
    {
        public class AddUser
        {            
            public IActorRef SessionRef { get; set; }
            public string RemoteAddress { get; set; }
        }
        public class DeleteUser
        {            
            public string RemoteAddress { get; set; }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////// Field
        ////////////////////////////////////////////////////////////////////////////////////////// Private
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// 액터 참조 해시 세트
        /// </summary>

        // 원격에 연결된 User Acotr들
        private readonly ConcurrentDictionary<string, User> _userList = new ConcurrentDictionary<string, User>();

        //private ICancelable _heartbeatTask;
        private IActorRef _dbCordiatorRef;

        public static IActorRef ActorOf(ActorSystem actorSystem, IActorRef dbCordiatorRef)
        {   
            var listenerProps = Props.Create(() => new WorldActor(dbCordiatorRef));
            return actorSystem.ActorOf(listenerProps, ActorPaths.World.Name);
        }


        /// <summary>
        /// 생성자
        /// </summary>
        public WorldActor(IActorRef dbCordiatorRef)
        {
            _dbCordiatorRef = dbCordiatorRef;

            Receive<WorldActor.AddUser> (
                addUser => {
                    OnRecvAddUser(addUser);
                }
            );
            Receive<WorldActor.DeleteUser>(
                deletedUser => {
                    OnRecvDeleteUser(deletedUser);
                }
            );

            Receive<AssociationErrorEvent>(e => HandleAssociationError(e));
            Receive<DisassociatedEvent>(e => HandleDisassociation(e));

            ReceiveAny(value =>
            {
                var senderPath = Sender.Path.ToString();
                if (_userList.TryGetValue(senderPath, out var findedUser))
                {
                    findedUser.UserRef.Tell(value);

                }
            });

            // 생존성 모니터링 시작
            //_heartbeatTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
            //    TimeSpan.Zero, TimeSpan.FromSeconds(5), Self, new Messages.Heartbeat(), Self);
        }

        /// <summary>
        /// AssociationErrorEvent: 
        /// 연결 설정이 실패했거나 연결 도중 오류가 발생했을 때 발생하는 이벤트입니다.
        /// 이 이벤트는 소켓이 닫히는 상황에서도 발생할 수 있습니다.
        /// </summary>
        /// <param name="e"></param>

        private void HandleAssociationError(AssociationErrorEvent e)
        {
            // AssociationErrorEvent contains the remote address in the RemoteAddress property
            var remoteAddress = e.RemoteAddress;
            // You can get the actor path from the remote address
            var remoteActorPath = remoteAddress + "/user/remoteActor";

            // handle error...
            _logger.Info($"HandleAssociationError:{remoteActorPath}");           
        }

        /// <summary>
        /// DisassociatedEvent: 연결이 끊어졌을 때 발생하는 이벤트입니다.
        /// 연결이 강제로 끊기거나 상대방이 연결을 종료했을 때 발생합니다.
        /// </summary>
        /// <param name="e"></param>
        private void HandleDisassociation(DisassociatedEvent e)
        {
            // DisassociatedEvent also contains the remote address in the RemoteAddress property
            var remoteAddress = e.RemoteAddress.ToString();
            // You can get the actor path from the remote address
            var remoteActorPath = remoteAddress + "/user/clientActor";

        }

        protected override void PreStart()
        {
            Context.System.EventStream.Subscribe(Self, typeof(AssociationErrorEvent));
            Context.System.EventStream.Subscribe(Self, typeof(DisassociatedEvent));
            base.PreStart();
        }

        protected override void PostStop()
        {
            Context.System.EventStream.Unsubscribe(Self, typeof(AssociationErrorEvent));
            Context.System.EventStream.Unsubscribe(Self, typeof(DisassociatedEvent));
                        
            _userList.Clear();

            // 생존성 모니터링 종료
            //_heartbeatTask?.Cancel();

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

        /// <summary>
        /// world에 유저 추가
        /// </summary>
        /// <param name="addUser"></param>
        private void OnRecvAddUser(WorldActor.AddUser addUser)
        {
            _logger.Debug($"OnRecvAddUser RemoteAddress:{addUser.RemoteAddress}, SessionRef:{addUser.SessionRef}");
            var user = User.Of(Context, Self, addUser.SessionRef);

            // 유저 추가
            _userList.TryAdd(addUser.RemoteAddress, user);
        }
                

        /// <summary>
        ///  world에서 유저 삭제
        /// </summary>
        /// <param name="user"></param>
        private void OnRecvDeleteUser(WorldActor.DeleteUser user)
        {
            var remoteAdress = user.RemoteAddress;
            if (_userList.TryGetValue(remoteAdress, out var finedUser))
            {
                Context.Stop(finedUser.UserRef);

                // remove the actor reference from the dictionary
                _userList.TryRemove(user.RemoteAddress, out var deleteUser);
            }
        }
    }
}