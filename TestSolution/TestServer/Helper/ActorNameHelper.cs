using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer.Helper
{
    static class ActorPath
    {
        public static readonly string System = "TestServer";
        public static readonly string Root = $"akka://{System}/user";

        // console Writer
        public static readonly string ConsoleReader = $"{Root}/consoleReaderActor";
    }
}
