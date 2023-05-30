using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer.Helper
{
    class ActorSupervisorHelper
    {
        private static readonly Lazy<ActorSupervisorHelper> lazy = new Lazy<ActorSupervisorHelper>(() => new ActorSupervisorHelper());

        public static ActorSupervisorHelper Instance { get { return lazy.Value; } }

        public IActorRef WorldCordiatorRef => _worldCordiatorRef;
        public IActorRef SessionCordiatorRef => _sessionCordiatorRef;
        public IActorRef DbCordiatorRef => _dbCordiatorRef;

        private IActorRef _worldCordiatorRef;
        private IActorRef _sessionCordiatorRef;
        private IActorRef _dbCordiatorRef;

        private ActorSupervisorHelper()
        {
            _worldCordiatorRef = null;
            _sessionCordiatorRef = null;
            _dbCordiatorRef = null;
        }

        public void SetWorldCordiatorRef(IActorRef worldCordiatorRef)
        {
            _worldCordiatorRef = worldCordiatorRef;
        }
        public void SetSessionCordiatorRef(IActorRef sessionCordiatorRef)
        {
            _sessionCordiatorRef = sessionCordiatorRef;
        }
        public void SetDbCordiatorRef(IActorRef dbCordiatorRef)
        {
            _dbCordiatorRef = dbCordiatorRef;
        }
    }
}
