using Dapper;
using GameServer.DataBase.MySql;
using GameServer.Helper;
using log4net;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component.DataBase
{
    public class MySqlDbComponent
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _connectionString;
        public MySqlDbComponent()
        {
            _connectionString = ConfigInstanceHelper.Instance.GameDbConnectionString;
        }

        private MySqlConnection ConnectionFactory()
        {
            //MySqlConnection의 연결 풀링 기능은 기본적으로 활성화되어 있습니다.
            //따라서 별도로 설정할 필요 없이, 연결 문자열을 사용하여 
            //MySqlConnection 객체를 만들면 자동으로 연결 풀링이 활용됩니다.
            // 아래와 같이 해야 MySql에서 설정한 DB 풀링 기능을 사용할 수 있습니다.
            // 싱글턴 객체로 가지고 있으면 연결 풀링 기능을 쓸 수 없다.
            
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
        /// userUid로 조회
        /// </summary>
        /// <param name="userUid"></param>
        /// <returns></returns>
        public TblUser GetUserInfo(ulong userUid)
        {            
            using (var db = ConnectionFactory())
            {
                var query = $"select * from tbl_user where user_uid={userUid} limit 1;";                                
                var tblUser = db.Query<TblUser>(query).FirstOrDefault();
                return tblUser != null ? tblUser : null;
            }            
        }
    }

    
}
