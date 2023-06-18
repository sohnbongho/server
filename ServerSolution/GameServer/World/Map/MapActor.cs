using Akka.Actor;
using GameServer.Helper;
using GameServer.Socket;
using log4net;
using Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static GameServer.World.Map.MapActor;

namespace GameServer.World.Map
{
    public class MapActor : UntypedActor
    {
        /// <summary>
        /// Map User표시
        /// </summary>
        private class MapUser
        {
            public IActorRef UserActorRef { get; set; }
            public IActorRef SessionActorRef { get; set; }
        }

        /// <summary>
        /// 맵입장
        /// </summary>
        public class EnterMapRequest
        {
            public int MapIndex { get; set; }
            public long UserUid { get; set; }
            public IActorRef UserActorRef { get; set; }
            public IActorRef UserSessionActorRef { get; set; }
        }

        public class EnterMapResponse
        {
            public int MapIndex { get; set; }
            public long UserUid { get; set; }
            public IActorRef MapActorRef { get; set; }
        }

        /// <summary>
        /// Map에서 떠남
        /// </summary>
        public class LeaveMapRequest
        {            
            public long UserUid { get; set; }            
        }

        /// <summary>
        /// 맵전체 유저에게 알림
        /// </summary>
        public class UserNotification
        {
            public MessageWrapper Message { get; set; }
        }


        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IActorRef _mapCordiator;
        private readonly int _mapIndex;
        private readonly Dictionary<long, MapUser> _userList = new ();

        public static IActorRef ActorOf(IUntypedActorContext context, IActorRef mapCordiator, int mapIndex)
        {
            var prop = Props.Create(() => new MapActor(mapCordiator, mapIndex));
            return context.ActorOf(prop);
        }
        public MapActor(IActorRef mapCordiator, int mapIndex)
        {
            _mapCordiator = mapCordiator;
            _mapIndex = mapIndex;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case MapActor.EnterMapRequest enterMapRequest:
                    {
                        _logger.Debug($"MapActor.enterMapRequest userUid:{enterMapRequest.UserUid}");

                        var userUid = enterMapRequest.UserUid;
                        /// 유저 추가
                        _userList[userUid] = new MapActor.MapUser
                        {
                            UserActorRef = enterMapRequest.UserActorRef,
                            SessionActorRef = enterMapRequest.UserSessionActorRef
                        };

                        // 맵 입장 성공
                        Sender.Tell(new MapActor.EnterMapResponse
                        {
                            MapIndex = _mapIndex, 
                            UserUid = userUid,
                            MapActorRef = Self
                        });
                        break;
                    }
                case MapActor.LeaveMapRequest leaveMapRequest:
                    {
                        // 유저 삭제
                        _logger.Debug($"MapActor.LeaveMapRequest userUid:{leaveMapRequest.UserUid}");

                        _userList.Remove(leaveMapRequest.UserUid);
                        break;
                    }
                case MapActor.UserNotification userNotification:
                    {
                        ///Map User들에게 알림
                        _logger.Debug($"MapActor.UserNotification");
                        foreach(var user in _userList.Values)
                        {
                            user.SessionActorRef.Tell(new SessionActor.SendMessage
                            {
                                Message = userNotification.Message
                            });

                        }
                        break;
                    }
                default:
                    break;
            }
        }
    }
}
