﻿using Akka.Actor;
using Akka.IO;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Library.Helper.Encrypt;
using GameServer.DataBase.MySql;
using GameServer.DataBase.Redis;
using GameServer.Helper;
using GameServer.Socket;
using GameServer.Manager;
using System.ComponentModel;
using MySqlConnector;
using GameServer.Component.User;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameServer.World.UserInfo
{
    public class User 
    {
        public IActorRef WorldRef{ get; private set; } // 나를 포함하고 있는 월드
        public IActorRef SessionRef { get; private set; } // 원격지 Actor
        public IActorRef UserRef { get; private set; } // 내가 속해 있는 유저

        public static User Of(IUntypedActorContext context, IActorRef worldActor, IActorRef sessionRef, string remoteAddress)
        {
            var props = Props.Create(() => new UserActor(worldActor, sessionRef, remoteAddress));
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
    public class UserActor : UntypedActor, IComponentManager
    {
        public class SessionReceiveData
        {
            public int MessageSize { get; set; }
            public byte[] RecvBuffer { get; set; }
        }

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
               
        private IActorRef _worldActor; // worldActor
        private IActorRef _sessionRef; // sessionActor
        private IActorRef _dbActorRef; // dbActor
        private IActorRef _redisActorRef; // redisActor

        private string _remoteAddress;
        
        private long _userUid = 0; // 
        private string _userId = string.Empty; // 
        

        // 필수 함수 핸들러들 (FSM이 변경되도 반드시 있어야 하는 핸들러들)
        private Dictionary<System.Type, Action<object, IActorRef>> _systemHandlers;

        private Dictionary<System.Type, Action<object, IActorRef>> _sessionHandlers;

        private Dictionary<MessageWrapper.PayloadOneofCase, Action<object, IActorRef>> _userHandlers;


        // Component 패턴
        private ComponentManager _componentManager = new ComponentManager();

        /// <summary>
        /// Component 관리
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        public void AddComponent<T>(T component) where T : class
        {
            _componentManager.AddComponent<T>(component);
        }

        public T GetComponent<T>() where T : class
        {
            return _componentManager.GetComponent<T>();
        }

        public void RemoveComponent<T>() where T : class
        {
            _componentManager.RemoveComponent<T>();
        }

        /// <summary>
        /// Akka관련
        /// </summary>
        /// <param name="worldActor"></param>
        /// <param name="sessionRef"></param>
        /// <param name="remoteAdress"></param>
        public UserActor(IActorRef worldActor, IActorRef sessionRef, string remoteAdress)
        {
            _worldActor = worldActor;
            _sessionRef = sessionRef;
            _remoteAddress = remoteAdress;

            _dbActorRef = null;
            _redisActorRef = null;

            // 
            // 생성과 동시에 메서드를 등록하는 dictionary
            _systemHandlers = new Dictionary<System.Type, Action<object, IActorRef>> {                
                {typeof(DbServiceCordiatorActor.UserToDbLinkResponse), (data, sender) => OnRecvDbLink((DbServiceCordiatorActor.UserToDbLinkResponse)data)},
                {typeof(RedisServiceCordiatorActor.UserToDbLinkResponse), (data, sender) => OnRecvRedisLink((RedisServiceCordiatorActor.UserToDbLinkResponse)data)},
                {typeof(GameDbServiceActor.SelectResponse), (data, sender) => OnRecvSelectReponse((GameDbServiceActor.SelectResponse)data)},
                {typeof(RedisServiceActor.StringGetResponse), (data, sender) => OnRecvRedisReponse((RedisServiceActor.StringGetResponse)data)},
                {typeof(SessionActor.UserToSessionLinkResponse), (data, sender) => OnRecvUserToSessionLinkResponse((SessionActor.UserToSessionLinkResponse)data)},
            };
            
            // 세션관련 
            _sessionHandlers = new Dictionary<System.Type, Action<object, IActorRef>>{
                {typeof(UserActor.SessionReceiveData), (data, sender) => OnRecvPacket((UserActor.SessionReceiveData)data, sender)},
            };

            // 유저 관련 핸들러
            _userHandlers = new Dictionary<MessageWrapper.PayloadOneofCase, Action<object, IActorRef>>
            {
                {MessageWrapper.PayloadOneofCase.ServerEnterRequest, (data, sender) => OnRecvServerEnterRequest((MessageWrapper)data, sender)},
                {MessageWrapper.PayloadOneofCase.SayRequest, (data, sender) => OnRecvSayRequest((MessageWrapper)data, sender)},
            };


            // Select 는 Compoent에서 처리하자 
            // update, delete는 actor에서 처리하자.
            // TODO: 추후에 update, delete도 Component에서 할지도?
            AddComponent<MySqlDbComponent>(new MySqlDbComponent()); // MySql연결
            AddComponent<RedisCacheComponent>(new RedisCacheComponent()); // Redis연결            
        }        

        protected override void PreStart()
        {
            base.PreStart();

            // SessionActor와 UserActor의 연결
            _sessionRef.Tell(new SessionActor.UserToSessionLinkRequest
            {
                UserRef = Self
            });
        }

        protected override void PostStop()
        {
            // Compone 제거
            RemoveComponent<MySqlDbComponent>();
            RemoveComponent<RedisCacheComponent>();

            Close();
            base.PostStop();
        }


        /// <summary>
        /// DB Actor와 연결
        /// </summary>
        /// <param name="received"></param>
        private void OnRecvDbLink(DbServiceCordiatorActor.UserToDbLinkResponse received)
        {
            _logger.Debug($"OnRecvDbLink - {received.DbActorRef}");
            _dbActorRef = received.DbActorRef;

            // 레디스 액터 연결 요청
            // redisCordiatorActor에 나에게 맞는 dbActor요청
            var redisCordiatorRef = ActorSupervisorHelper.Instance.RedisCordiatorRef;
            redisCordiatorRef?.Tell(new RedisServiceCordiatorActor.UserToDbLinkRequest
            {
                UserActorRef = Self
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

            // 연결 준비가 완료되었다.
            // 메시지를 받을 준비가 되었다.
            var conntedMessage = new MessageWrapper
            {
                ConnectedResponse = new ConnectedResponse
                {
                }
            };
            Tell(conntedMessage);
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
                    Close();

                    return Directive.Stop;                    
                });
        }

        /// <summary>
        /// 유저가 비정상 일때 종료 요청
        /// </summary>
        private void Close()
        {
            // Session Actor에 요청하여 종료 처리
            var sessionCordiatorRef = ActorSupervisorHelper.Instance.SessionCordiatorRef;
            sessionCordiatorRef.Tell(new SessionCordiatorActor.ClosedRequest
            {
                RemoteAdress = _remoteAddress
            });
        }

        /// <summary>
        /// User와 Session액터 연결 성공
        /// </summary>
        /// <param name="data"></param>
        private void OnRecvUserToSessionLinkResponse(SessionActor.UserToSessionLinkResponse data)
        {
            // dbCordiatorActor에 나에게 맞는 dbActor요청
            var dbCordiatorRef = ActorSupervisorHelper.Instance.DbCordiatorRef;
            dbCordiatorRef?.Tell(new DbServiceCordiatorActor.UserToDbLinkRequest
            {
                UserActorRef = Self
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
        protected override void OnReceive(object message)
        {
            var dataType = message.GetType();
            if (_systemHandlers.TryGetValue(dataType, out var handler))
            {
                handler(message, Sender);
                return;
            }
            
            if (_sessionHandlers.TryGetValue(dataType, out var userHandler))
            {
                userHandler(message, Sender);
                return;
            }
            else
            {
                // 핸들러를 찾지 못했을 때의 처리
                // ...
                Unhandled(message);
            }
        }
        private void Tell(MessageWrapper message)
        {
            var json = JsonConvert.SerializeObject(message);
            _logger.Info($"Tell - message({message.PayloadCase.ToString()}) data({json})");

            var res = new SessionActor.SendMessage
            {
                Message = message
            };
            _sessionRef.Tell(res);
        }

        private void BroardcastTell(MessageWrapper message)
        {
            var json = JsonConvert.SerializeObject(message);
            _logger.Info($"BroardcastTell- message({message.PayloadCase.ToString()}) data({json})");

            var res = new SessionCordiatorActor.BroadcastMessage
            {
                Message = message
            };
            _sessionRef.Tell(res);
        }        

        /// <summary>
        /// Redis 에서 온값들 
        /// </summary>
        /// <param name="data"></param>
        private void OnRecvRedisReponse(RedisServiceActor.StringGetResponse data)
        {
            switch(data.RedisCallId)
            {
                case RedisServiceActor.RedisCallId.ServerSessionId:
                    {
                        break;
                    }
            }
        }

        

        /// <summary>
        /// 유저들에게 패킷
        /// </summary>
        /// <param name="received"></param>
        /// <param name="sessionRef"></param>
        private bool OnRecvPacket(UserActor.SessionReceiveData received, IActorRef sessionRef)
        {
            byte[] receivedMessage = received.RecvBuffer;

            // 전체를 관리하는 wapper로 변환 역직렬화
            MessageWrapper wrapper = MessageWrapper.Parser.ParseFrom(receivedMessage);
            var json = JsonConvert.SerializeObject(wrapper);

            _logger.Info($"OnRecvPacket - message({wrapper.PayloadCase}) data({json})");
                        
            if(_userHandlers.TryGetValue(wrapper.PayloadCase, out var handler))
            {
                handler(wrapper, Sender);
                return true;
            }
            
            return false;
        }

        private void OnRecvServerEnterRequest(MessageWrapper wrapper, IActorRef sessionRef)
        {
            var request = wrapper.ServerEnterRequest;
            var redis = GetComponent<RedisCacheComponent>();
            var db = GetComponent<MySqlDbComponent>();

            var dicts = redis.GetSessionToUserUid(request.SessionKey);
            long userUid = 0;            

            if (dicts.TryGetValue("user_uid", out var obj1))
            {
                userUid = long.Parse(obj1.ToString());
            }            

            var userId =  db.GetUserInfo(userUid).user_id;

            _userUid = userUid;
            _userId = userId;

            // 클라이언트에게 알림
            var response = new MessageWrapper
            {
                ServerEnterResponse = new ServerEnterResponse
                {
                    UserUid = userUid,
                    UserId = userId
                }
            };
            Tell(response);
        }

        private void OnRecvSayRequest(MessageWrapper wrapper, IActorRef sessionRef)
        {
            var request = wrapper.SayRequest;

            var response = new MessageWrapper
            {
                SayResponse = new SayResponse
                {
                    UserId = request.UserId,
                    Message = request.Message
                }
            };
            BroardcastTell(response);
        }


    }
}
