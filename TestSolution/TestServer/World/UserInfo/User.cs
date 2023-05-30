using Akka.Actor;
using Akka.IO;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestLibrary;
using TestServer.DataBase;
using TestServer.DataBase.Entities;
using TestServer.Helper;
using TestServer.Socket;

namespace TestServer.World.UserInfo
{
    public class User 
    {
        public IActorRef WorldRef{ get; private set; } // 나를 포함하고 있는 월드
        public IActorRef SessionRef { get; private set; } // 원격지 Actor
        public IActorRef UserRef { get; private set; } // 내가 속해 있는 유저

        public static User Of(IUntypedActorContext context, IActorRef worldActor, IActorRef sessionRef)
        {
            var props = Props.Create(() => new UserActor(worldActor, sessionRef));
            var userActor = context.ActorOf(props);

            return new User(worldActor, sessionRef, userActor);
        }

        public User(IActorRef worldActor, IActorRef sessionRef, IActorRef userActor)
        {
            WorldRef = worldActor;
            SessionRef = sessionRef;
            UserRef = userActor;
        }
    }

    /// <summary>
    /// User Actor
    /// </summary>
    public class UserActor : ReceiveActor, ILogReceive
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
               
        private IActorRef _worldActor; // worldActor
        private IActorRef _sessionRef; // sessionActor
        private IActorRef _dbActorRef; // dbActor        

        public UserActor(IActorRef worldActor, IActorRef sessionRef)
        {
            _worldActor = worldActor;
            _sessionRef = sessionRef;
            _dbActorRef = null;

            Receive< Tcp.Received > (
             received =>
             {
                 OnRecvPacket(received, Sender);
             });

            Receive<DbServiceCordiatorActor.UserToDbLinkResponse>(
             received =>
             {
                 OnRecvDbLink(received);
             });

            Receive<GameDbServiceActor.SelectResponse>(
            received =>
            {
                OnRecvSelectReponse(received);
            });


        }
        
        protected override void PreStart()
        {
            // SessionActor와 UserActor의 연결
            _sessionRef.Tell(new SessionActor.UserToSessionLinkRequest
            {
                UserRef = Self
            });

            // dbCordiatorActor에 나에게 맞는 dbActor요청
            var dbCordiatorRef = ActorSupervisorHelper.Instance.DbCordiatorRef;
            dbCordiatorRef?.Tell(new DbServiceCordiatorActor.UserToDbLinkRequest
            {
                UserActorRef = Self
            });

            base.PreStart();

        }

        protected override void PostStop()
        {
            _logger.Debug("UserActor PostStop");
            base.PostStop();
        }

        // here we are overriding the default SupervisorStrategy
        // which is a One-For-One strategy w/ a Restart directive
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, // maxNumberOfRetries
                TimeSpan.FromSeconds(5), // duration
                x =>
                {
                    return Directive.Restart;

                    ////Maybe we consider ArithmeticException to not be application critical
                    ////so we just ignore the error and keep going.
                    //if (x is ArithmeticException) return Directive.Resume;

                    ////Error that we cannot recover from, stop the failing actor
                    //else if (x is NotSupportedException) return Directive.Stop;

                    ////In all other cases, just restart the failing actor
                    //else return Directive.Restart;
                });
        }
        private void Tell(GenericMessage message)
        {
            var res = new SessionActor.SendMessage
            {
                Message = message
            };
            _sessionRef.Tell(res);
        }

        private void BroardcastTell(GenericMessage message)
        {
            var res = new SessionCordiatorActor.BroadcastMessage
            {
                Message = message
            };
            _sessionRef.Tell(res);
        }


        private void OnRecvPacket(Tcp.Received received, IActorRef sessionRef)
        {
            // 받은 패킷을 유저 actor에 보낸다.
            var messageObject = GenericMessage.FromByteArray(received.Data.ToArray());            

            switch (messageObject)
            {
                case SayRequest sayRequest:
                    {
                        _logger.Debug($"SayRequest - {sayRequest.UserName} : {sayRequest.Message}");                        
                        var message = new SayResponse
                        {
                            UserName = sayRequest.UserName,
                            Message = sayRequest.Message
                        };
                        BroardcastTell(message);
                        
                        break;
                    }
            }
        }

        /// <summary>
        /// DB Actor와 연결
        /// </summary>
        /// <param name="received"></param>
        private void OnRecvDbLink(DbServiceCordiatorActor.UserToDbLinkResponse received)
        {
            _dbActorRef = received.DbActorRef;

            // User정보 요청
            var userUid = 1001;
            var query = $"select * from tbl_user where user_uid={userUid};";
            _dbActorRef.Tell(new GameDbServiceActor.SelectRequest {
                Query = query,
                TblType = typeof(TblUser)
            });
        }

        /// <summary>
        /// DB요청에 대한 응답
        /// </summary>
        /// <param name="received"></param>
        private void OnRecvSelectReponse(GameDbServiceActor.SelectResponse received)
        {
            var users = received.Results.Select(x => TblUser.Of(x)).ToList();
            foreach(var user in users)
            {                
                _logger.Info($"select user - {user.seq}, {user.user_uid}, {user.user_id}, {user.level}");
            }
        }
    }
}
