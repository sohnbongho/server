using System;
using System.IO;
using System.Reflection;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using log4net;
using TestServer.World;
using TestServer.ConsoleActor;
using TestServer.Socket;

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
            var config = LoadAkkaHCONConfig();

            using (ActorSystem actorSystem = ActorSystem.Create("TestServer", config))
            {
                //Akka.Remote로 초기화
                var worldActor = WorldActor.ActorOf(actorSystem);

                // Akka.IO로 초기화                
                var client = ListenerActor.ActorOf(actorSystem, worldActor, 8081);

                

                // text console창에 적는 actor                
                //var consoleWriterActor = ConsoleWriterActor.ActorOf(actorSystem);

                //// text를 읽는 actor                
                //var consoleReaderActor = ConsoleReaderActor.ActorOf(actorSystem, consoleWriterActor);
                //consoleReaderActor.Tell(ConsoleReaderActor.StartCommand); // begin processing

                _logger.Info(@"Server Doing. ""exit"" is exit");

                // blocks the main thread from exiting until the actor system is shut down
                actorSystem.WhenTerminated.Wait();
            }
        }

        /// <summary>
        /// HCON 파일을 읽어온다.
        /// </summary>
        /// <returns></returns>
        private static Config LoadAkkaHCONConfig()
        {
            var fullPath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(fullPath);

            string path = $@"{directoryPath}\AkkaHCON.conf"; // 수정해야 할 부분
                                                        // ConfigurationFactory.ParseString(hocon);

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