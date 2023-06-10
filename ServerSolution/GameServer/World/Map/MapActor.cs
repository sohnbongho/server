using Akka.Actor;
using GameServer.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.World.Map
{
    public class MapActor : UntypedActor
    {
        private IActorRef _mapCordiator;
        public static IActorRef ActorOf(IUntypedActorContext context, IActorRef mapCordiator)
        {
            var prop = Props.Create(() => new MapActor(mapCordiator));
            return context.ActorOf(prop);
        }
        public MapActor(IActorRef mapCordiator)
        {
            _mapCordiator = mapCordiator;
        }

        protected override void OnReceive(object message)
        {

        }
    }
}
