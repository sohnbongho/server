using System;
using System.IO;
using System.Reflection;
using Akka.Actor;
using Akka.Configuration;
using log4net;
using TestServer.World;
using TestServer.ConsoleActor;
using TestServer.Socket;
using TestServer.Helper;
using TestServer.DataBase.MySql;
using TestServer.DataBase.Redis;

namespace TestServer
{
    /// <summary>
    /// 프로그램
    /// </summary>
    class Program
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Method
        ////////////////////////////////////////////////////////////////////////////////////////// Static
        //////////////////////////////////////////////////////////////////////////////// Private
        ///
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 프로그램 시작하기
        /// </summary>
        //private static void Main()
        //{
        //    // Akka HOCON 정보 읽어오기
        //    var config = LoadAkkaHCONConfig();

        //    using (ActorSystem actorSystem = ActorSystem.Create("TestServer", config))
        //    {
        //        var listenerProps = WorldActor.ActorOf(actorSystem);

        //        // text console창에 적는 actor                
        //        var consoleWriterActor = ConsoleWriterActor.ActorOf(actorSystem);

        //        // text를 읽는 actor                
        //        var consoleReaderActor = ConsoleReaderActor.ActorOf(actorSystem, consoleWriterActor);
        //        consoleReaderActor.Tell(ConsoleReaderActor.StartCommand); // begin processing

        //        _logger.Info(@"Server Doing. ""exit"" is exit");

        //        // blocks the main thread from exiting until the actor system is shut down
        //        actorSystem.WhenTerminated.Wait();
        //    }
        //}
        private static void Main()
        {
            // Akka HOCON 정보 읽어오기
            Config config = LoadAkkaHconConfig();

            ConfigInstanceHelper.Instance.Load(); // config파일 읽어오기

            using (ActorSystem actorSystem = ActorSystem.Create("TestServer", config))
            {
                // text console창에 적는 actor                
                var consoleWriterActor = ConsoleWriterActor.ActorOf(actorSystem);

                /// text를 읽는 actor                
                var consoleReaderActor = ConsoleReaderActor.ActorOf(actorSystem, consoleWriterActor);
                consoleReaderActor.Tell(ConsoleReaderActor.StartCommand); // begin processing

                // Db actor                
                var dbActor = DbServiceCordiatorActor.ActorOf(actorSystem);
                ActorSupervisorHelper.Instance.SetDbCordiatorRef(dbActor);

                // Redis actor                
                var redisActor = RedisServiceCordiatorActor.ActorOf(actorSystem);
                ActorSupervisorHelper.Instance.SetRedisCordiatorRef(redisActor);

                // World Actor 생성                
                var worldActor = WorldActor.ActorOf(actorSystem, dbActor);
                ActorSupervisorHelper.Instance.SetWorldCordiatorRef(worldActor);

                // Akka.IO로 초기화                
                var client = ListenerActor.ActorOf(actorSystem, worldActor, ConfigInstanceHelper.Instance.Port);                

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