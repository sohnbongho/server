using Akka.Actor;
using GameServer.DataBase.MySql;
using GameServer.Helper;
using GameServer.World.UserInfo;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.World.Map
{
    public class MapCordiatorActor : UntypedActor
    {   

        private IActorRef _worldRef;
        private readonly int[] _mapIndexes = { 1001, 1002, 1003 };
        private readonly ConcurrentDictionary<int, IActorRef> _maps = new();

        public static IActorRef ActorOf(IUntypedActorContext context, IActorRef worldRef)
        {
            var prop = Props.Create(() => new MapCordiatorActor(worldRef));
            return context.ActorOf(prop, ActorPaths.MapCordiator.Name);
        }

        public MapCordiatorActor(IActorRef worldRef)
        {
            _worldRef = worldRef;            
        }

        /// <summary>
        /// map들 생성
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            _maps.Clear();
            foreach (var mapIndex in _mapIndexes)
            {
                var mapActor = MapActor.ActorOf(Context, Self, mapIndex);
                _maps.TryAdd(mapIndex, mapActor);

            }
        }

        protected override void OnReceive(object message)
        {
            switch (message) 
            { 
                case MapActor.EnterMapRequest enterMapRequest:
                    {
                        if(_maps.TryGetValue(enterMapRequest.MapIndex, out var mapActor))
                        {
                            mapActor.Tell(enterMapRequest, Sender);
                        }

                        break;
                    }
            }
        }
    }
}
