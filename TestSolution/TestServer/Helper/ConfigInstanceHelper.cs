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
            return true;
        }

        /// <summary>
        /// MySQL 관련 정보
        /// </summary>
        /// <returns></returns>
        public string GetGameDbConnectionString()
        {
            return _jsonObj["Db"]["MySql"]["ConnectString"]["GameDb"].ToString();
        }


        public int GetDbPoolCount()
        {
            return _jsonObj["Db"]["MySql"]["PoolCount"].Value<int>();
        }

        /// <summary>
        /// Redis 관련 정보
        /// </summary>
        /// <returns></returns>
        public string GetRedisConnectionString()
        {
            return _jsonObj["Db"]["Redis"]["ConnectString"].ToString();
        }
        public int GetRedisPoolCount()
        {
            return _jsonObj["Db"]["Redis"]["PoolCount"].Value<int>();
        }
    }
}
