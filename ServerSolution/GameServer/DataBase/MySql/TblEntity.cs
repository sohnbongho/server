using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 반드시 DB와 변수명이 동일해야 한다.
/// </summary>
namespace GameServer.DataBase.MySql
{
    public class TblUser
    {
        public long seq { get; set; }
        public long user_uid { get; set; }
        public string user_id { get; set; }
        public int level { get; set; }
        public static TblUser Of(dynamic obj)
        {
            return new TblUser
            {
                seq = obj.seq,
                user_uid = obj.user_uid,
                user_id = obj.user_id,
                level = obj.level,
            };
        }
    }
}
