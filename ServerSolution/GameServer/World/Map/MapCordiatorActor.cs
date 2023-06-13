using Akka.Actor;
using GameServer.DataBase.MySql;
using GameServer.Helper;
using GameServer.World.UserInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.World.Map
{
    public class MapCordiatorActor : UntypedActor
    {
        private IActorRef _worldRef;
        private readonly int[] _maps = { 1001, 1002, 1003 };

        public static IActorRef ActorOf(IUntypedActorContext context, IActorRef worldRef)
        {
            var prop = Props.Create(() => new MapCordiatorActor(worldRef));
            return context.ActorOf(prop, ActorPaths.MapCordiator.Name);
        }

        public MapCordiatorActor(IActorRef worldRef)
        {
            _worldRef = worldRef;
            //var actorRef = Context.ActorSelection($"{address}/{path}");
        }

        /// <summary>
        /// map들 생성
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();
            
        }

        protected override void OnReceive(object message)
        {

        }
    }
}
