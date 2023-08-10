using Akka.Actor;
using Akka.Configuration;
using log4net;
using LoginServer.ConsoleActor;
using LoginServer.DataBase.MySql;
using LoginServer.DataBase.Redis;
using LoginServer.Helper;
using LoginServer.Socket;
using LoginServer.World;
using System.Reflection;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Hosting;

namespace LoginServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "LoginServer API",
                    Description = "Request LoginServer",
                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Example Contact",
                        Url = new Uri("https://example.com/contact")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Example License",
                        Url = new Uri("https://example.com/license")
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                //var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                //var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                //c.IncludeXmlComments(xmlPath);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
                c.RoutePrefix = string.Empty;
            });
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
    internal class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
        }

        static void Main(string[] args)
        {
            // Akka HOCON 정보 읽어오기            
            //if (false == ConfigInstanceHelper.Instance.Load())
            //{
            //    throw new Exception("failed to config load");
            //}
            
            var akkaTask = Task.Run(() => RunAkkaSystem());
            var hostTask = Task.Run(() => CreateHostBuilder(args).Build().Run());
            Task.WhenAll(akkaTask, hostTask).Wait();
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

        private static void RunAkkaSystem()
        {
            Config config = LoadAkkaHconConfig();

            using (ActorSystem actorSystem = ActorSystem.Create(ActorPaths.System, config))
            {
                // text console창에 적는 actor                
                var consoleWriterActor = ConsoleWriterActor.ActorOf(actorSystem);

                /// text를 읽는 actor                
                var consoleReaderActor = ConsoleReaderActor.ActorOf(actorSystem, consoleWriterActor);
                consoleReaderActor.Tell(ConsoleReaderActor.StartCommand); // begin processing                                                                          // 

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
    }
}