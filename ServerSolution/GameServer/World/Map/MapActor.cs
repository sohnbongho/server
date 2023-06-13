using Akka.Actor;
using GameServer.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.World.Map
{
    public class MapActor : UntypedActor
    {
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

        private class MapUser
        {
            public IActorRef UserActorRef { get; set; }
            public IActorRef SessionActorRef { get; set; }
        }

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
                        var userUid = enterMapRequest.UserUid;
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
            }
        }
    }
}
