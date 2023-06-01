using Akka.Actor;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestServer.Helper;

namespace TestServer.DataBase.Redis
{
    public class RedisServiceCordiatorActor : ReceiveActor
    {
        public class UserToDbLinkRequest
        {
            public IActorRef UserActorRef;
        }
        public class UserToDbLinkResponse
        {
            public IActorRef RedisActorRef;
        }

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly int _actorCount = 1;
        private int _actorPos = 0;
        private object _actorPosLock = new object();

        private readonly ConcurrentDictionary<string, IActorRef> _redises = new ConcurrentDictionary<string, IActorRef>();

        public static IActorRef ActorOf(ActorSystem actorSystem)
        {
            var consoleReaderProps = Props.Create(() => new RedisServiceCordiatorActor());
            return actorSystem.ActorOf(consoleReaderProps, ActorPaths.RedisCordiator.Name);
        }

        public RedisServiceCordiatorActor()
        {
            _actorCount = ConfigInstanceHelper.Instance.GetRedisPoolCount();
            _logger.Info($"RedisServiceCordiatorActor poolCount:({_actorCount })");

            Receive<RedisServiceCordiatorActor.UserToDbLinkRequest>(message =>
            {
                OnReceiveUserToDbLinkRequest(message);
            });


        }
        protected override void PreStart()
        {
            base.PreStart();

            _actorPos = 0;
            for (var i = 0; i < _actorCount; i++)
            {
                var dbName = $"{ActorPaths.Redis.Name}{i}";

                var dbRef = RedisServiceActor.ActorOf(Context, Self, dbName);
                _redises.TryAdd(dbName, dbRef);
            }
        }
        private void OnReceiveUserToDbLinkRequest(RedisServiceCordiatorActor.UserToDbLinkRequest message)
        {
            var userActorRef = message.UserActorRef;
            var dbName = string.Empty;
            // 하나씩 증가 시키자
            lock (_actorPosLock)
            {
                dbName = $"{ActorPaths.Redis.Name}{_actorPos}";
                ++_actorPos;
                _actorPos = _actorPos >= _actorCount ? 0 : _actorPos;
            }

            if (_redises.TryGetValue(dbName, out var dbActorRef))
            {
                userActorRef.Tell(new RedisServiceCordiatorActor.UserToDbLinkResponse
                {
                    RedisActorRef = dbActorRef
                });
            }
        }
    }
}
