using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Helper
{
    /// <summary>
    /// Config를 관리하는 싱글턴 객체
    /// </summary>
    public sealed class ConfigInstanceHelper
    {
        private static readonly Lazy<ConfigInstanceHelper> lazy = new Lazy<ConfigInstanceHelper>(() => new ConfigInstanceHelper());

        public static ConfigInstanceHelper Instance { get { return lazy.Value; } }

        private JObject _jsonObj = null;

        private int _port = 0;
        private bool _packetEncrypt = true;

        private string _gameDbConnectionString;
        private int _dbPoolCount;
        
        private string _redisConnectString;
        private int _redisPoolCount;

        public int Port => _port;

        public bool PacketEncrypt => _packetEncrypt;

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

            _port = _jsonObj["remote"]["port"].Value<int>();
            _packetEncrypt = _jsonObj["remote"]["encrypt"].Value<bool>();

            _gameDbConnectionString = _jsonObj["db"]["mySql"]["connectString"]["gameDb"].ToString();
            _dbPoolCount = _jsonObj["db"]["mySql"]["poolCount"].Value<int>();

            _redisConnectString = _jsonObj["db"]["redis"]["connectString"].ToString();
            _redisPoolCount = _jsonObj["db"]["redis"]["poolCount"].Value<int>();

            return true;
        }
    }
}
