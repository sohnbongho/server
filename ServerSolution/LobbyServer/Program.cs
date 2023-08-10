using Akka.Actor;
using Akka.Configuration;
using LobbyServer.ConsoleActor;
using LobbyServer.DataBase.MySql;
using LobbyServer.DataBase.Redis;
using LobbyServer.Helper;
using LobbyServer.Socket;
using LobbyServer.World;
using log4net;
using System.Reflection;

namespace LobbyServer
{
    internal class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            // Akka HOCON 정보 읽어오기
            Config config = LoadAkkaHconConfig();
            if(false == ConfigInstanceHelper.Instance.Load())
            {
                throw new Exception("failed to config load");
            }

            using (ActorSystem actorSystem = ActorSystem.Create(ActorPaths.System, config))
            {
                // text console창에 적는 actor                
                var consoleWriterActor = ConsoleWriterActor.ActorOf(actorSystem);

                /// text를 읽는 actor                
                var consoleReaderActor = ConsoleReaderActor.ActorOf(actorSystem, consoleWriterActor);
                consoleReaderActor.Tell(ConsoleReaderActor.StartCommand); // begin processing

                // Db actor                
                var dbActor = DbServiceCordiatorActor.ActorOf(actorSystem);

                // Redis actor                
                var redisActor = RedisServiceCordiatorActor.ActorOf(actorSystem);

                // World Actor 생성                
                var worldActor = WorldActor.ActorOf(actorSystem);

                // Akka.IO로 초기화                
                var listener = ListenerActor.ActorOf(actorSystem, worldActor, ConfigInstanceHelper.Instance.Port);

                _logger.Info($@"Port:{ConfigInstanceHelper.Instance.Port} Server Doing. ""exit"" is exit");

                // blocks the main thread from exiting until the actor system is shut down
                actorSystem.WhenTerminated.Wait();
            }
        }
        /// <summary>
        /// HCON 파일을 읽어온다.
        /// </summary>
        /// <returns></returns>
        private static Config LoadAkkaHconConfig()
        {
            var fullPath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(fullPath);

            string path = $@"{directoryPath}\AkkaHcon.conf"; // 수정해야 할 부분

            // 파일이 존재하는지 확인
            if (File.Exists(path) == false)
            {
                _logger.Error($"not found file : {path}");
                Config tmpConfig = new Config();
                return tmpConfig;
            }
            // 파일 내용 읽기
            string content = File.ReadAllText(path);

            var config = ConfigurationFactory.ParseString(content);
            return config;
        }
    }
}