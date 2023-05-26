using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer.Helper
{
    public static class ActorStatus
    {
        //public static bool CheckedRoundRobine(ActorSystem actorSystem, IActorRef actor)
        //{            
        //    var router = actor as RouterActor;

        //    if (router != null)
        //    {
        //        var routee = router.Routees.First();
        //        var deployment = actorSystem.Provider.GetDeploy(routee.Path);

        //        if (deployment.RouterConfig is RoundRobinPool)
        //        {
        //            Console.WriteLine("This actor is using the RoundRobinPool routing strategy.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("This actor is not using the RoundRobinPool routing strategy.");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("This actor is not a router.");
        //    }

        //    return true;
            
        //}
    }
}
