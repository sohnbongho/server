using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Helper
{
    public static class ActorPaths
    {
        public static readonly string System = "GameServer";
        public static readonly string Root = $"akka://{System}/user";        

        // console Writer        
        public static readonly ActorMetaData ReaderConsole = new ActorMetaData("readerConsole");
        public static readonly ActorMetaData WriterConsole = new ActorMetaData("writerConsole");

        /*--------------------------------------
         * System관련
         --------------------------------------*/
        // MySql DB Actor
        public static readonly ActorMetaData DbCordiator = new ActorMetaData("dbcordiator");
        public static readonly ActorMetaData GameDb = new ActorMetaData("gamedb", DbCordiator);

        // Redis Actor
        public static readonly ActorMetaData RedisCordiator = new ActorMetaData("rediscordiator");
        public static readonly ActorMetaData Redis = new ActorMetaData("redis", DbCordiator);

        // 유저 Session Actor
        public static readonly ActorMetaData Listener = new ActorMetaData("listener");
        public static readonly ActorMetaData SessionCordiator = new ActorMetaData("sessioncordiator");
        public static readonly ActorMetaData Session = new ActorMetaData("session", SessionCordiator);

        /*--------------------------------------
         * World관련
         --------------------------------------*/
        public static readonly ActorMetaData World = new ActorMetaData("world");

        // User관리
        public static readonly ActorMetaData UserCordiator = new ActorMetaData("usercordiator", World);
        public static readonly ActorMetaData User = new ActorMetaData("user", UserCordiator);        

        // Map
        public static readonly ActorMetaData MapCordiator = new ActorMetaData("mapcordiator", World);
        public static readonly ActorMetaData Map = new ActorMetaData("map", MapCordiator);


    }

    public class ActorMetaData
    {
        public ActorMetaData(string name, ActorMetaData parent = null)
        {            
            Name = name;
            Parent = parent;
            var parentPath = parent != null ? parent.Path : ActorPaths.Root;
            Path = $"{parentPath}/{Name}";

            var relativeParentPath = parent != null ? parent.RelativePath : string.Empty;
            RelativePath = $"{relativeParentPath}/{Name}";

        }
        public string Name { get; private set; }
        public ActorMetaData Parent { get; private set; }
        public string Path { get; private set; }
        public string RelativePath { get; private set; }

    }
}
