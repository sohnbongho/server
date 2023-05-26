using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer.Helper
{
    public static class ActorPaths
    {
        public static readonly string System = "TestServer";
        public static readonly string Root = $"akka://{System}/user";        

        // console Writer        
        public static readonly ActorMetaData ReaderConsole = new ActorMetaData("readerConsole");
        public static readonly ActorMetaData WriterConsole = new ActorMetaData("writerConsole");

        public static readonly ActorMetaData Listener = new ActorMetaData("listener");
        public static readonly ActorMetaData SessionCordiator = new ActorMetaData("sessionCordiator");

        // Remote에서 온 메시지 처리하는 actor
        public static readonly ActorMetaData World = new ActorMetaData("world");
        public static readonly ActorMetaData User = new ActorMetaData("user", World);
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
