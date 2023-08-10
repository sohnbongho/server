using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.DBTables
{
    public class TblServerList
    {
        public int server_id { get; set; }
        public int world_id { get; set; }
        public short server_type { get; set; }
        public string server_name { get; set; }
        public string ipaddr { get; set; }
        public int port { get; set; }
        public int level_min { get; set; }
        public int level_max { get; set; }
        public string parameter { get; set; }
        public static TblServerList Of(dynamic obj)
        {
            return new TblServerList
            {
                server_id = obj.server_id,
                world_id = obj.world_id,
                server_type = obj.server_type,
                server_name = obj.server_name,
                ipaddr = obj.ipaddr,
                port = obj.port,
                level_min = obj.level_min,
                level_max = obj.level_max,
                parameter = obj.parameter,
            };
        }
    }
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

    public class TblCharacter
    {
        public ulong user_seq { get; set; }
        public ulong char_seq { get; set; }
        public string user_id { get; set; }
        public short gender { get; set; }
        public short char_class { get; set; }
        public string nickname { get; set; }
        public string alias { get; set; }
        public ulong exp { get; set; }
        public short level { get; set; }
        public int login_count { get; set; }
        public int login_todaycount { get; set; }
        public int user_cash { get; set; }
        public int game_money { get; set; }
        public int game_heart { get; set; }
        public int game_star { get; set; }
        public int myroom_UV { get; set; }
        public int ranking_private { get; set; }
        public int ranking_myroom { get; set; }
        public DateTime creation_date { get; set; }
        public int byClass { get; set; }
        public int head_parts { get; set; }
        public int face_parts { get; set; }
        public int upperbody_parts { get; set; }
        public int lowerbody_parts { get; set; }
        public int shoes_parts { get; set; }
        public string myroom_name { get; set; }
        public string introduce { get; set; }
        public string birthday { get; set; }
        public ulong couple_uid { get; set; }
        public ulong bestfriend_uid { get; set; }
        public DateTime last_login { get; set; }
        public DateTime last_logout { get; set; }
        public short login_flag { get; set; }
        public short inven_level { get; set; }
        public int fatigue_level { get; set; }
        public int fatigue_time { get; set; }
        public int birthday_year { get; set; }
        public int birthday_date { get; set; }
        public int background_item { get; set; }
        public int skin_item { get; set; }
        public int pose_item { get; set; }

        public static TblCharacter Of(dynamic obj)
        {
            return new TblCharacter
            {
                user_seq = obj.user_seq,
                char_seq = obj.char_seq,
                user_id = obj.user_id,
                gender = obj.gender,
                char_class = obj.char_class,
                nickname = obj.nickname,
                alias = obj.alias,
                exp = obj.exp,
                level = obj.level,
                login_count = obj.login_count,
                login_todaycount = obj.login_todaycount,
                user_cash = obj.user_cash,
                game_money = obj.game_money,
                game_heart = obj.game_heart,
                game_star = obj.game_star,
                myroom_UV = obj.myroom_UV,
                ranking_private = obj.ranking_private,
                ranking_myroom = obj.ranking_myroom,
                creation_date = obj.creation_date,
                byClass = obj.byClass,
                head_parts = obj.head_parts,
                face_parts = obj.face_parts,
                upperbody_parts = obj.upperbody_parts,
                lowerbody_parts = obj.lowerbody_parts,
                shoes_parts = obj.shoes_parts,
                myroom_name = obj.myroom_name,
                introduce = obj.introduce,
                birthday = obj.birthday,
                couple_uid = obj.couple_uid,
                bestfriend_uid = obj.bestfriend_uid,
                last_login = obj.last_login,
                last_logout = obj.last_logout,
                login_flag = obj.login_flag,
                inven_level = obj.inven_level,
                fatigue_level = obj.fatigue_level,
                fatigue_time = obj.fatigue_time,
                birthday_year = obj.birthday_year,
                birthday_date = obj.birthday_date,
                background_item = obj.background_item,
                skin_item = obj.skin_item,
                pose_item = obj.pose_item,
            };
        }
    }
    public class TblInvenAccessory
    {
        public ulong user_seq { get; set; }
        public ulong char_seq { get; set; }
        public int item_seq { get; set; }
        public int item_type { get; set; }
        public ulong item_uid { get; set; }
        public short is_use { get; set; }
        public short is_hold { get; set; }
        public ulong couple_uid { get; set; }
        public DateTime expiration_date { get; set; }
        public short favorites { get; set; }
        
    }

    public class TblInvenSet
    {
        public ulong user_seq { get; set; }
        public ulong char_seq { get; set; }
        public int item_seq { get; set; }
        public int item_type { get; set; }
        public ulong item_uid { get; set; }
        public short is_use { get; set; }
        public short is_hold { get; set; }
        public int byClass { get; set; }
        public ulong couple_uid { get; set; }
        public DateTime expiration_date { get; set; }
        public short favorites { get; set; }
    }

    public class TblInvenItem
    {
        public ulong user_seq { get; set; }
        public ulong char_seq { get; set; }
        public int item_seq { get; set; }
        public int item_type { get; set; }
        public ulong item_uid { get; set; }
        public short is_use { get; set; }
        public short is_hold { get; set; }
        public int byClass { get; set; }
        public ulong couple_uid { get; set; }
        public DateTime expiration_date { get; set; }
        public short favorites { get; set; }
    }
}
