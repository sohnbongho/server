using System;
using Akka.Actor;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using log4net;
using System.Reflection;
using Akka.IO;
using Akka.Event;
using LobbyServer.Helper;
using LobbyServer.World.UserInfo;
using LobbyServer.World.Map;

namespace LobbyServer.World
{
    /// <summary>
    /// 채팅 서버 액터
    /// </summary>
    public class WorldActor : UntypedActor
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Field
        ////////////////////////////////////////////////////////////////////////////////////////// Private
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);        
        
        private IActorRef _userCordiatorRef = null;
        private IActorRef _mapCordiatorRef = null;

        public static IActorRef ActorOf(ActorSystem actorSystem)
        {   
            var listenerProps = Props.Create(() => new WorldActor());
            return actorSystem.ActorOf(listenerProps, ActorPaths.World.Name);
        }


        /// <summary>
        /// 생성자
        /// </summary>
        public WorldActor()
        {
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

        //private void HandleAssociationError(AssociationErrorEvent e)
        //{
        //    // AssociationErrorEvent contains the remote address in the RemoteAddress property
        //    var remoteAddress = e.RemoteAddress;
        //    // You can get the actor path from the remote address
        //    var remoteActorPath = remoteAddress + "/user/remoteActor";

        //    // handle error...
        //    _logger.Info($"HandleAssociationError:{remoteActorPath}");           
        //}

        /// <summary>
        /// DisassociatedEvent: 연결이 끊어졌을 때 발생하는 이벤트입니다.
        /// 연결이 강제로 끊기거나 상대방이 연결을 종료했을 때 발생합니다.
        /// </summary>
        /// <param name="e"></param>
        //private void HandleDisassociation(DisassociatedEvent e)
        //{
        //    // DisassociatedEvent also contains the remote address in the RemoteAddress property
        //    var remoteAddress = e.RemoteAddress.ToString();
        //    // You can get the actor path from the remote address
        //    var remoteActorPath = remoteAddress + "/user/clientActor";

        //}

        protected override void PreStart()
        {
            //Context.System.EventStream.Subscribe(Self, typeof(AssociationErrorEvent));
            //Context.System.EventStream.Subscribe(Self, typeof(DisassociatedEvent));
            base.PreStart();

            _userCordiatorRef = UserCordiatorActor.ActorOf(Context, Self);
            _mapCordiatorRef = MapCordiatorActor.ActorOf(Context, Self);
        }

        protected override void PostStop()
        {
            //Context.System.EventStream.Unsubscribe(Self, typeof(AssociationErrorEvent));
            //Context.System.EventStream.Unsubscribe(Self, typeof(DisassociatedEvent));

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

        protected override void OnReceive(object message)
        {            
        }
    }
}