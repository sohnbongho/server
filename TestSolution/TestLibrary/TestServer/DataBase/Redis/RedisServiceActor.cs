using Akka.Actor;
using log4net;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestServer.Helper;

// 참조 사이트:
// http://egloos.zum.com/sweeper/v/3157497

namespace TestServer.DataBase.Redis
{
    public class RedisServiceActor : ReceiveActor
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IActorRef _redisCordiator;
        private readonly string _name;        
                
        /// <summary>
        /// Redis에 저장
        /// </summary>
        public class StringSet
        {
            public RedisConnectorHelper.DataBaseId DataBaseId { get; set; } = RedisConnectorHelper.DataBaseId.Default;
            public string Key { get; set; } = string.Empty;
            public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
        }

        /// <summary>
        /// 레디스 값얻어오기
        /// </summary>
        public class StringGetRequest
        {
            public RedisConnectorHelper.DataBaseId DataBaseId { get; set; } = 0;
            public string Key { get; set; } = string.Empty;            
        }
        public class StringGetResponse
        {
            public RedisConnectorHelper.DataBaseId DataBaseId { get; set; } = 0;
            public string Key { get; set; } = string.Empty;
            public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
        }

        private class User
        {
            public long Uid { get; set; }
            public string Name { get; set; }
        }

        public static IActorRef ActorOf(IUntypedActorContext context, IActorRef redisCordiator, string name)
        {
            var prop = Props.Create(() => new RedisServiceActor(redisCordiator, name));
            return context.ActorOf(prop, name);
        }

        public RedisServiceActor(IActorRef redisCordiator, string name)
        {
            _redisCordiator = redisCordiator;
            _name = name;
            Receive<RedisServiceActor.StringSet>(message =>
            {
                OnRecvStringSet(message);
            });
            Receive<RedisServiceActor.StringGetRequest>(message =>
            {
                OnRecvStringGet(message, Sender);
            });
        }
        protected override void PreStart()
        {
            base.PreStart();
            
        }
        
        /// <summary>
        /// 레디스에 저장
        /// </summary>
        /// <param name="message"></param>
        private void OnRecvStringSet(RedisServiceActor.StringSet message)
        {            
            var redis = RedisConnectorHelper.Connection;
            var dbId = (int)message.DataBaseId;            
            var db = redis.GetDatabase(dbId);

            var key = message.Key;
            var values = message.Values;

            var hashEntries = values.Select(d => new HashEntry(d.Key, ConvertHelper.ConvertObjectToRedisValue(d.Value))).ToArray();            
            db.HashSet(key, hashEntries); //set하는 함수
        }

        /// <summary>
        /// 레디스 값 읽어오기
        /// </summary>
        /// <param name="message"></param>
        private void OnRecvStringGet(RedisServiceActor.StringGetRequest message, IActorRef sender)
        {
            var redis = RedisConnectorHelper.Connection;
            var dbId = (int)message.DataBaseId;
            var db = redis.GetDatabase(dbId);
            var key = message.Key;

            HashEntry[] vals = db.HashGetAll(key);
            foreach (var value in vals)
            {
                _logger.Debug($"vals = {value.Name}:: {value.Value}");
            }

            Dictionary<string, object> dicts 
                = vals.ToDictionary(x => x.Name.ToString(), x => ConvertHelper.ConvertRedisValueToObject(x.Value));

            sender.Tell(new RedisServiceActor.StringGetResponse {
                DataBaseId = message.DataBaseId,
                Values = dicts
            });
        }


    }
}
