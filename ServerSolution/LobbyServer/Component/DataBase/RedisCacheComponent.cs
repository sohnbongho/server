using LobbyServer.DataBase.Redis;
using LobbyServer.Helper;
using log4net;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static RedisConnectorHelper;

namespace LobbyServer.Component.DataBase
{
    public class RedisCacheComponent
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Session 정보로 UserInfo 요청
        /// </summary>
        /// <param name="session"></param>
        public Dictionary<string, object>  GetSessionToUserUid(string session)
        {
            var redis = RedisConnectorHelper.Connection;
            var dbId = (int)DataBaseId.Session;
            var db = redis.GetDatabase(dbId);
            var key = session;

            HashEntry[] vals = db.HashGetAll(key);            

            return vals.ToDictionary(x => x.Name.ToString(), 
                x => ConvertHelper.ConvertRedisValueToObject(x.Value));            
        }       
    }
}
