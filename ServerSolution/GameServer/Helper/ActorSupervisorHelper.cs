using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Helper
{
    /// <summary>
    /// Cordiator들 Ref만 모아둔 싱글턴 객체이다.
    /// </summary>
    class ActorSupervisorHelper
    {
        private static readonly Lazy<ActorSupervisorHelper> lazy = new Lazy<ActorSupervisorHelper>(() => new ActorSupervisorHelper());
        public static ActorSupervisorHelper Instance { get { return lazy.Value; } }

        public IActorRef WorldCordiatorRef => _worldCordiatorRef;
        public IActorRef SessionCordiatorRef => _sessionCordiatorRef;
        public IActorRef DbCordiatorRef => _dbCordiatorRef;
        public IActorRef RedisCordiatorRef => _redisCordiatorRef;
        public IActorRef ListenerRef => _listenerRef;

        public IActorRef UserCordiatorRef => _userCordiatorRef;
        public IActorRef MapCordiatorRef => _mapCordiatorRef;


        private IActorRef _sessionCordiatorRef = null;
        private IActorRef _dbCordiatorRef = null;
        private IActorRef _redisCordiatorRef = null;
        private IActorRef _listenerRef = null;

        private IActorRef _worldCordiatorRef = null;
        private IActorRef _userCordiatorRef = null;
        private IActorRef _mapCordiatorRef = null;

        private ActorSupervisorHelper()
        {            
        }
        /*------------------------------------
         * System Cordiator
         ------------------------------------*/        
        public void SetSessionCordiatorRef(IActorRef sessionCordiatorRef)
        {
            _sessionCordiatorRef = sessionCordiatorRef;
        }
        public void SetDbCordiatorRef(IActorRef dbCordiatorRef)
        {
            _dbCordiatorRef = dbCordiatorRef;
        }

        public void SetRedisCordiatorRef(IActorRef redisCordiatorRef)
        {
            _redisCordiatorRef = redisCordiatorRef;
        }

        public void SetListenerRef(IActorRef listenerRef)
        {
            _listenerRef = listenerRef;
        }

        /*------------------------------------
         * 콘텐츠 (World) Cordiator
         ------------------------------------*/
        public void SetWorldCordiatorRef(IActorRef worldCordiatorRef)
        {
            _worldCordiatorRef = worldCordiatorRef;
        }

        public void SetUserCordiatorRef(IActorRef userCordiatorRef)
        {
            _userCordiatorRef = userCordiatorRef;
        }

        public void SetMapCordiatorRef(IActorRef mapCordiatorRef)
        {
            _mapCordiatorRef = mapCordiatorRef;
        }
    }
}
