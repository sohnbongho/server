using Akka.Actor;
using Dapper;
using log4net;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LoginServer.Helper;
using LoginServer.Socket;

namespace LoginServer.DataBase.MySql
{
    public class GameDbServiceActor : ReceiveActor
    {
        // User 정보 요청
        public class SelectRequest
        {
            public string Query { get; set; } = string.Empty;
            public Type TblType { get; set; }
        }
        public class SelectResponse
        {
            public string Query { get; set; } = string.Empty;
            public List<dynamic> Results { get; set; } = new List<dynamic>();
            public Type TblType { get; set; }
        }


        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IActorRef _dbCordiator;
        private readonly string _name;
        private readonly string _connectionString;

        public static IActorRef ActorOf(IUntypedActorContext context, IActorRef dbCordiator, string name)
        {
            var prop = Props.Create(() => new GameDbServiceActor(dbCordiator, name));
            return context.ActorOf(prop, name);
        }
        public GameDbServiceActor(IActorRef dbCordiator, string name)
        {
            _dbCordiator = dbCordiator;
            _name = name;
            _connectionString = ConfigInstanceHelper.Instance.GameDbConnectionString;

            Receive<GameDbServiceActor.SelectRequest>(message =>
            {
                OnReceiveExcuteSelectRequest(message);
            });

        }
        protected override void PreStart()
        {
            base.PreStart();

            CheckDatabaseStatus();

        }

        /// <summary>
        /// actor 종료
        /// </summary>
        protected override void PostStop()
        {
            _logger.Info($"GameDbServiceActor.poststop() - name({_name})");            

            base.PostStop();

        }

        private MySqlConnection ConnectionFactory()
        {
            //MySqlConnection의 연결 풀링 기능은 기본적으로 활성화되어 있습니다.
            //따라서 별도로 설정할 필요 없이, 연결 문자열을 사용하여 
            //MySqlConnection 객체를 만들면 자동으로 연결 풀링이 활용됩니다.
            // 아래와 같이 해야 MySql에서 설정한 DB 풀링 기능을 사용할 수 있습니다.
            var mySqlConnection = new MySqlConnection(_connectionString);
            try
            {
                mySqlConnection.Open();
            }
            catch (Exception e)
            {
                _logger.Error($"error connectionfactory - connectionString({_connectionString})", e);
                return null;
            }
            return mySqlConnection;
        }

        /// <summary>
        /// Data Base의 상태를 얻어온다.
        /// </summary>
        private void CheckDatabaseStatus()
        {
            using (var db = ConnectionFactory())
            {
                if (db == null)
                    _logger.Error("fail database connect");

                var query = "SELECT NOW()";
                var  now = db.Query(query).ToList();

                // Reflection 사용 하여 형변환 강제화
                //response.Results = ExecuteQuery(db, request.TblType, request.Query).ToList();
            }
        }

        /// <summary>
        /// Select에 대한 응답
        /// </summary>
        /// <param name="request"></param>
        private void OnReceiveExcuteSelectRequest(GameDbServiceActor.SelectRequest request)
        {
            var response = new GameDbServiceActor.SelectResponse{                
                Query = request.Query,              
                TblType = request.TblType
            };
            using (var db = ConnectionFactory())
            {
                // Reflection사용 안하고 받는 쪽에서 형변환
                response.Results = db.Query(request.Query).ToList();
                
                // Reflection 사용 하여 형변환 강제화
                //response.Results = ExecuteQuery(db, request.TblType, request.Query).ToList();
            }
            Sender.Tell(response);
        }

        /// <summary>
        /// Reflection으로 강제로 형변환 시도
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="type"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public IEnumerable<object> ExecuteQuery(MySqlConnection connection, Type type, string sql)
        {
            var commandDefinition = new CommandDefinition(sql);
            var genericMethodDefinition = typeof(SqlMapper).GetMethods()
            .FirstOrDefault(method => method.Name == "Query"
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 1
                && method.GetParameters().Length == 2);

            var genericMethod = genericMethodDefinition.MakeGenericMethod(type);
            return (IEnumerable<object>)genericMethod.Invoke(null, new object[] { connection, commandDefinition });
        }

    }
}
