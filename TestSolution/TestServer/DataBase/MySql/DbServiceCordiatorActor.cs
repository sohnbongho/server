using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestServer.ConsoleActor;
using TestServer.Helper;
using Dapper;
using MySqlConnector;
using TestServer.World.UserInfo;
using TestServer.Socket;
using System.Collections.Concurrent;
using log4net;
using System.Reflection;

// https://gist.github.com/jacking75/635ece4395b6f9073d8ae575c346fa83

namespace TestServer.DataBase.MySql
{
    public class DbServiceCordiatorActor : ReceiveActor
    {
        // User Actor에서 db 액터에 대한 정보 요청
        public class UserToDbLinkRequest
        {
            public IActorRef UserActorRef;
        }
        public class UserToDbLinkResponse
        {
            public IActorRef DbActorRef;
        }

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly int _actorCount = 1;
        private int _actorPos = 0;
        private object _actorPosLock = new object();

        private readonly ConcurrentDictionary<string, IActorRef> _dbs = new ConcurrentDictionary<string, IActorRef>();

        public static IActorRef ActorOf(ActorSystem actorSystem)
        {
            var consoleReaderProps = Props.Create(() => new DbServiceCordiatorActor());
            return actorSystem.ActorOf(consoleReaderProps, ActorPaths.DbCordiator.Name);
        }

        public DbServiceCordiatorActor()
        {
            _actorCount = ConfigInstanceHelper.Instance.DbPoolCount;
            _logger.Info($"DbServiceCordiatorActor poolCount:({_actorCount })");

            Receive<DbServiceCordiatorActor.UserToDbLinkRequest>(message =>
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
                var dbName = $"{ActorPaths.GameDb.Name}{i}";

                var dbRef = GameDbServiceActor.ActorOf(Context, Self, dbName);
                _dbs.TryAdd(dbName, dbRef);
            }
        }

        private void OnReceiveUserToDbLinkRequest(DbServiceCordiatorActor.UserToDbLinkRequest message)
        {
            var userActorRef = message.UserActorRef;
            var dbName = string.Empty;
            // 하나씩 증가 시키자
            lock (_actorPosLock)
            {   
                dbName = $"{ActorPaths.GameDb.Name}{_actorPos}";
                ++_actorPos;
                _actorPos = _actorPos >= _actorCount ? 0 : _actorPos;
            }

            if (_dbs.TryGetValue(dbName, out var dbActorRef))
            {
                userActorRef.Tell(new DbServiceCordiatorActor.UserToDbLinkResponse
                {
                    DbActorRef = dbActorRef
                });
            }
        }        
    }    
}
