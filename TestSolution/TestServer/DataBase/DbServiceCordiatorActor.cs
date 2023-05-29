using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestServer.ConsoleActor;
using TestServer.Helper;
using Dapper;
using MySqlConnector;
using TestServer.World.UserInfo;

// https://gist.github.com/jacking75/635ece4395b6f9073d8ae575c346fa83

namespace TestServer.DataBase
{
    public class DbServiceCordiatorActor : ReceiveActor
    {
        public class SelectRequest
        {
            public string Query { get; set; }
        }

        public class SelectResponse
        {
            public List<object> Results { get; set; }
        }

        public static IActorRef ActorOf(ActorSystem actorSystem)
        {
            var consoleReaderProps = Props.Create(() => new DbServiceCordiatorActor());
            return actorSystem.ActorOf(consoleReaderProps, ActorPaths.Db.Name);
        }

        public class Game
        {            
            public long seq { get; set; }
            public long user_uid { get; set; }
            public string user_id { get; set; }
            public int level { get; set; }
        }


        public DbServiceCordiatorActor()
        {
            Receive<SelectRequest>(
             selectRequest =>
             {
                 using (var connection = ConnectionFactory())
                 {
                     var result = connection.Query<Game>(
                             "select * from user where user_uid=@useruid;",
                             new { useruid = 1001});

                     Console.WriteLine("-- simple mapping:" + result.Count());

                     foreach (var p in result)
                     {
                         Console.WriteLine($"{p.seq} {p.user_uid} {p.user_id} {p.level}");
                     }
                 }
             }
         );

        }
        MySqlConnection ConnectionFactory()
        {
            string connectionString = "host=127.0.0.1;port=3306;userid=root;password=1111;database=game;";
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }

    
}
