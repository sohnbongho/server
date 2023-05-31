using Akka.Actor;
using Akka.IO;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TestServer.DataBase.MySql;
using TestServer.DataBase.MySql.MySql.Entities;
using TestServer.DataBase.Redis;
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
        public class SessionReceiveData
        {
            public byte[] RecvBuffer { get; set; }
        }

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
               
        private IActorRef _worldActor; // worldActor
        private IActorRef _sessionRef; // sessionActor
        private IActorRef _dbActorRef; // dbActor
        private IActorRef _redisActorRef; // redisActor

        private ulong _userUid = 1001; // TODO: 추후에 UserUid를 갱신하는 로직을 넣자.

        public UserActor(IActorRef worldActor, IActorRef sessionRef)
        {
            _worldActor = worldActor;
            _sessionRef = sessionRef;
            _dbActorRef = null;
            _redisActorRef = null;

            Receive<UserActor.SessionReceiveData> (
             received =>
             {
                 OnRecvPacket(received, Sender);
             });

            Receive<DbServiceCordiatorActor.UserToDbLinkResponse>(
             received =>
             {
                 OnRecvDbLink(received);
             });

            Receive<RedisServiceCordiatorActor.UserToDbLinkResponse>(
             received =>
             {
                 OnRecvRedisLink(received);
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

            // redisCordiatorActor에 나에게 맞는 dbActor요청
            var redisCordiatorRef = ActorSupervisorHelper.Instance.RedisCordiatorRef;
            redisCordiatorRef?.Tell(new RedisServiceCordiatorActor.UserToDbLinkRequest
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
        private void Tell(MessageWrapper message)
        {           
            var res = new SessionActor.SendMessage
            {
                Message = message
            };
            _sessionRef.Tell(res);
        }

        private void BroardcastTell(MessageWrapper message)
        {
            var res = new SessionCordiatorActor.BroadcastMessage
            {
                Message = message
            };
            _sessionRef.Tell(res);
        }


        private void OnRecvPacket(UserActor.SessionReceiveData received, IActorRef sessionRef)
        {
            var receivedMessage = received.RecvBuffer;

            // 전체를 관리하는 wapper로 변환 역직렬화
            var wrapper = MessageWrapper.Parser.ParseFrom(receivedMessage);
            _logger.Debug($"OnRecvPacket {wrapper.PayloadCase.ToString()}");

            switch (wrapper.PayloadCase)
            {
                case MessageWrapper.PayloadOneofCase.SayRequest:
                    {
                        var request = wrapper.SayRequest;
                        var response = new MessageWrapper {
                               SayResponse = new SayResponse
                               {
                                   Id = request.Id,
                                   User = request.User,
                                   Message = request.Message
                               }
                           };
                        BroardcastTell(response);

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
            _logger.Debug($"OnRecvDbLink - {received.DbActorRef}");

            _dbActorRef = received.DbActorRef;

            // User정보 요청            
            var query = $"select * from tbl_user where user_uid={_userUid};";
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
            TblUser user = received.Results.Select(x => TblUser.Of(x)).FirstOrDefault();
            if (user is null)
                return;
            _logger.Debug($"OnRecvSelectReponse {user.seq}, {user.user_uid}");

            // DB에서 온 값을 그대로 redis에 갱신한다.
            var dict = ConvertHelper.ConvertToDictionary(user);
            var key = $"{ActorPaths.User.Name}{user.user_uid}";

            _redisActorRef?.Tell(new RedisServiceActor.StringSet
            {
                DataBaseId = RedisConnectorHelper.DataBaseId.User,
                Key = key,
                Values = dict
            });
        }

        /// <summary>
        /// Redis 연결 액터
        /// </summary>
        /// <param name="received"></param>
        private void OnRecvRedisLink(RedisServiceCordiatorActor.UserToDbLinkResponse received)
        {
            _logger.Debug($"OnRecvRedisLink - {received.RedisActorRef}");

            _redisActorRef = received.RedisActorRef;

        }
    }
}
