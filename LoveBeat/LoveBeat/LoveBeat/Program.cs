using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using log4net;
using Newtonsoft.Json.Linq;

namespace LoveBeat
{
    internal static class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(nameof(Program));
        

        private static void Main(string[] args)
        {
            _logger.Info("Start Server");
            Console.WriteLine("Start Server ");
            // http 커넥션 관리용 콜백 설정
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;

            try
            {
                // Ctrl-C 키 핸들러 등록. 눌리면 actor system 종료한다.
                var cancellationTokenSource = new CancellationTokenSource();

                // 서버 시작
                using var actorSystem = Start(args);

                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    cancellationTokenSource.Cancel();
                };

                try
                {
                    actorSystem.WhenTerminated.Wait(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.Warn("cancelled server");
                }


            }
            catch(Exception e)
            {
                _logger.Error("main error", e);
            }
        }
                
        private static ActorSystem Start(string[] cmdLineArgs)
        {
            // 액터 시스템 생성
            string path = Assembly.GetExecutingAssembly().Location;
            string currentDirectory = Path.GetDirectoryName(path);
            string fileName = "akka.config";
            var configPath = Path.Combine(currentDirectory, fileName);

            _logger.Warn($"Start Akka system ({configPath})");
            
            var akkaConfig = LoadAkkaConfig(configPath);

            var actorSystem = CreateActorSystem(akkaConfig);            

            // Stage.Tell()에서 사용가능하도록 role 연동을 설정한다.
            Stage.SetActorWithRoles(SupportHelper.ActorWithRoles);

            // akka config에 따라 모든 role 액터 생성
            CreateRoleActors(akkaConfig, actorSystem);
            
           
            return actorSystem;
        }
        /// <summary>
        /// akkaConfig로부터 이름을 얻어 ActorSystem을 생성한다
        /// </summary>
        private static ActorSystem CreateActorSystem(Config akkaConfig)
        {            
            var actorSystemName = $"LoveBeatM";
            return ActorSystem.Create(actorSystemName, akkaConfig);
        }
        

        private static Akka.Configuration.Config LoadAkkaConfig(string configPath)
        {
            var fileContent = File.ReadAllText(configPath);

            _logger.Warn($"AkkaConfig-File({fileContent})");

            var akkaConfig = ConfigurationFactory.ParseString(fileContent);

            string actorRoles = "gamegate";
            string remoteRoles = "login";
            string remoteBindHostname = "0,0,0,0";
            int remoteBindPort = 2552;
            string remotePublicHostname = "127.0.0.1";

            // server_config의 shutdown_db_commit_timeout 설정된 값에서 10초 더한 후에
            // akka의 coordinated-shutdown.phases.service-unbind.timeout에 설정
            string shutdownTimeout = (600 + 10) + "s";

            _logger.Info($"shutdownTimeout:{shutdownTimeout}");

            akkaConfig = ConfigurationFactory.ParseString($"akka.actor.roles = {actorRoles}")
                .WithFallback(ConfigurationFactory.ParseString($"akka.remote.roles = {remoteRoles}"))
                .WithFallback(ConfigurationFactory.ParseString($"akka.remote.dot-netty.tcp.bind-hostname = {remoteBindHostname}"))
                .WithFallback(ConfigurationFactory.ParseString($"akka.remote.dot-netty.tcp.bind-port = {remoteBindPort}"))
                .WithFallback(ConfigurationFactory.ParseString($"akka.remote.dot-netty.tcp.port = {remoteBindPort}"))
                .WithFallback(ConfigurationFactory.ParseString($"akka.remote.dot-netty.tcp.public-hostname = {remotePublicHostname}"))
                .WithFallback(ConfigurationFactory.ParseString($"akka.coordinated-shutdown.phases.service-unbind.timeout = {shutdownTimeout}"))
                .WithFallback(akkaConfig);

            // 참고: meta 파라미터에 akkaConfig 를 넘기지 않고, akkaConfig.ToString() 을 사용한다.
            _logger.Warn($"AkkaConfig-Config ({akkaConfig.ToString(includeFallback: true)})");

            return akkaConfig;
        }

    }
}
