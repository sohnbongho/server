using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestServer.Helper
{
    /// <summary>
    /// Config를 관리하는 싱글턴 객체
    /// </summary>
    public sealed class ConfigInstanceHelper
    {
        private static readonly Lazy<ConfigInstanceHelper> lazy = new Lazy<ConfigInstanceHelper>(() => new ConfigInstanceHelper());

        public static ConfigInstanceHelper Instance { get { return lazy.Value; } }

        private JObject _jsonObj = null;

        private string _gameDbConnectionString;
        private int _dbPoolCount;
        
        private string _redisConnectString;
        private int _redisPoolCount;


        public string GameDbConnectionString => _gameDbConnectionString;
        public int DbPoolCount => _dbPoolCount;

        public string RedisConnectString =>_redisConnectString;
        public int RedisPoolCount => _redisPoolCount;


        private ConfigInstanceHelper()
        {
        }
        public bool Load()
        {
            var fullPath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(fullPath);

            string filePath = $@"{directoryPath}\Config.json5"; // 수정해야 할 부분
            string jsonString = File.ReadAllText(filePath);

            // Parse JSON string to JObject using Newtonsoft.Json
            _jsonObj = JObject.Parse(jsonString);

            _gameDbConnectionString = _jsonObj["Db"]["MySql"]["ConnectString"]["GameDb"].ToString();
            _dbPoolCount = _jsonObj["Db"]["MySql"]["PoolCount"].Value<int>();

            _redisConnectString = _jsonObj["Db"]["Redis"]["ConnectString"].ToString();
            _redisPoolCount = _jsonObj["Db"]["Redis"]["PoolCount"].Value<int>();

            return true;
        }
    }
}
