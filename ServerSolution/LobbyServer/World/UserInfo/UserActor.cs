﻿using Akka.Actor;
using log4net;
using Messages;
using Newtonsoft.Json;
using System.Reflection;
using LobbyServer.DataBase.MySql;
using LobbyServer.DataBase.Redis;
using LobbyServer.Helper;
using LobbyServer.Socket;
using LobbyServer.Component.DataBase;
using LobbyServer.Component;
using LobbyServer.World.Map;
using System.Reflection.Metadata.Ecma335;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Library.DBTables;
using StackExchange.Redis;
using Google.Protobuf.WellKnownTypes;
using Library.messages;

namespace LobbyServer.World.UserInfo
{
    public class User 
    {
        public IActorRef UserCordiatorActor { get; private set; } // 나를 포함하고 있는 월드
        public IActorRef SessionRef { get; private set; } // 원격지 Actor
        public IActorRef UserRef { get; private set; } // 내가 속해 있는 유저

        public static User Of(IUntypedActorContext context, IActorRef userCordiatorActor, 
            IActorRef sessionRef, string remoteAddress)
        {
            var props = Props.Create(() => new UserActor(userCordiatorActor, sessionRef, remoteAddress));
            var userActor = context.ActorOf(props);

            return new User(userCordiatorActor, sessionRef, userActor);
        }

        public User(IActorRef userCordiatorActor, IActorRef sessionRef, IActorRef userActor)
        {
            UserCordiatorActor = userCordiatorActor;
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
               
        private IActorRef _userCordiatorActor; // userCordiatorActor
        private IActorRef _sessionRef; // sessionActor
        private IActorRef _dbActorRef; // dbActor
        private IActorRef _redisActorRef; // redisActor

        // 내가 속해있는 맵 actor
        public IActorRef _mapActorRef { get; private set; } = ActorRefs.Nobody;

        private string _remoteAddress;
        
        private ulong _userSeq = 0; // 
        

        // 필수 함수 핸들러들 (FSM이 변경되도 반드시 있어야 하는 핸들러들)
        private Dictionary<System.Type, Action<object, IActorRef>> _systemHandlers;

        private Dictionary<System.Type, Action<object, IActorRef>> _sessionHandlers;

        private Dictionary<MessageWrapper.PayloadOneofCase, Action<MessageWrapper, IActorRef>> _userHandlers;


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
        public UserActor(IActorRef userCordiatorActor, IActorRef sessionRef, string remoteAdress)
        {
            _userCordiatorActor = userCordiatorActor;
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
                {typeof(MapActor.EnterMapResponse), (data, sender) => OnRecvMapEnterResonponse((MapActor.EnterMapResponse)data, sender)},
            };

            // 유저 관련 핸들러
            _userHandlers = new Dictionary<MessageWrapper.PayloadOneofCase, Action<MessageWrapper, IActorRef>>
            {
                {MessageWrapper.PayloadOneofCase.EnterLobbyRequest, (data, sender) => OnRecvEnterLobbyRequest(data, sender)},
                {MessageWrapper.PayloadOneofCase.SayRequest, (data, sender) => OnRecvSayRequest(data, sender)},
                {MessageWrapper.PayloadOneofCase.EquipItemRequest, (data, sender) => OnRecvEquipItemRequest(data, sender)},
                {MessageWrapper.PayloadOneofCase.InvenRequest, (data, sender) => OnRecvInvenRequest(data, sender)},
            };
            
        }        

        protected override void PreStart()
        {
            base.PreStart();

            // Select 는 Compoent에서 처리하자 
            // update, delete는 actor에서 처리하자.
            // TODO: 추후에 update, delete도 Component에서 할지도?
            var connectString = ConfigInstanceHelper.Instance.GameDbConnectionString;
            AddComponent<MySqlDbComponent>(new MySqlDbComponent(connectString)); // MySql연결
            AddComponent<RedisCacheComponent>(new RedisCacheComponent()); // Redis연결            

            // SessionActor와 UserActor의 연결
            _sessionRef.Tell(new SessionActor.UserToSessionLinkRequest
            {
                UserRef = Self
            });
        }

        protected override void PostStop()
        {
            // 맵을 떠남
            if(_mapActorRef != ActorRefs.Nobody)
            {
                _mapActorRef.Tell(new MapActor.LeaveMapRequest
                {
                    UserUid = _userSeq
                });
            }

            // Compone 제거
            RemoveComponent<MySqlDbComponent>();
            RemoveComponent<RedisCacheComponent>();
            _componentManager.Dispose();

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
            var redisCordiatorRef = Context.ActorSelection(ActorPaths.RedisCordiator.Path);

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
                    // Session Actor에 요청하여 종료 처리
                    Context.Stop(Self);

                    return Directive.Stop;                    
                });
        }
        

        /// <summary>
        /// User와 Session액터 연결 성공
        /// </summary>
        /// <param name="data"></param>
        private void OnRecvUserToSessionLinkResponse(SessionActor.UserToSessionLinkResponse data)
        {
            // dbCordiatorActor에 나에게 맞는 dbActor요청            
            var dbCordiatorRef = Context.ActorSelection(ActorPaths.DbCordiator.Path);

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

            // 핸들러를 찾지 못했을 때의 처리
            // ...
            Unhandled(message);
        }
        private void Tell(MessageWrapper message)
        {
            var json = JsonConvert.SerializeObject(message);
            _logger.Info($"Client<-Server - message({message.PayloadCase.ToString()}) data({json})");

            var res = new SessionActor.SendMessage
            {
                Message = message
            };
            _sessionRef.Tell(res);
        }

        private void TellMap(MessageWrapper message)
        {
            // 맵이 없다.
            if (_mapActorRef == ActorRefs.Nobody)
                return;
            
            var json = JsonConvert.SerializeObject(message);
            _logger.Info($"Client<-Server - message({message.PayloadCase.ToString()}) data({json})");

            var res = new MapActor.UserNotification
            {
                Message = message
            };
            _mapActorRef.Tell(res);
        }

        private void BroardcastTell(MessageWrapper message)
        {
            var json = JsonConvert.SerializeObject(message);
            _logger.Info($"Client<-Server - broadcast message({message.PayloadCase.ToString()}) data({json})");

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

            _logger.Info($"Client->Server - message({wrapper.PayloadCase}) data({json})");
                        
            if(_userHandlers.TryGetValue(wrapper.PayloadCase, out var handler))
            {
                handler(wrapper, Sender);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 로비 입장
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="sessionRef"></param>
        private void OnRecvEnterLobbyRequest(MessageWrapper wrapper, IActorRef sessionRef)
        {
            var request = wrapper.EnterLobbyRequest;

            var db = GetComponent<MySqlDbComponent>();

            _userSeq = request.UserSeq;
            
            var worldIndex = ConfigInstanceHelper.Instance.WorldId;
            var serverList = db.GetServerList(worldIndex, Library.messages.ServerType.Zone);

            // 존 정보들
            {
                var response = new MessageWrapper
                {
                    EnterLobbyResponse = new EnterLobbyResponse
                    {                        
                    }
                };
                foreach (var server in serverList)
                {
                    response.EnterLobbyResponse.ZoneInfos.Add(new ZoneInfo
                    {
                        Id = server.server_id,
                        Ip = server.ipaddr,
                        Name = server.server_name,
                        Port = server.port,
                    });
                }

                Tell(response);
            }           
        }
        
        /// <summary>
        /// 착장 정보
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="sessionRef"></param>
        private void OnRecvEquipItemRequest(MessageWrapper wrapper, IActorRef sessionRef)
        {
            var request = wrapper.EquipItemRequest;

            var db = GetComponent<MySqlDbComponent>();
            var tblCharacter = db.GetCharacterInfo(_userSeq);
            if (tblCharacter == null)
            {
                _logger.Error($"not found charter:{_userSeq}");
                return;
            }

            // 클라이언트에게 알림
            var response = new MessageWrapper
            {
                EquipItemResponse = new EquipItemResponse
                {
                    Nickname = tblCharacter.nickname,
                    Alias = tblCharacter.alias,
                    Class = tblCharacter.byClass,

                    Gender = tblCharacter.gender,
                    HeadParts = tblCharacter.head_parts,
                    FaceParts = tblCharacter.face_parts,
                    UpperbodyParts = tblCharacter.upperbody_parts,
                    LowerbodyParts = tblCharacter.lowerbody_parts,
                    ShoesParts = tblCharacter.shoes_parts,

                    Money = tblCharacter.game_money,
                    Heart = tblCharacter.game_heart,
                    Star = tblCharacter.game_star,
                    Cash = tblCharacter.user_cash,

                    Exp = tblCharacter.exp,
                    Level = tblCharacter.level,
                }
            };
            Tell(response);
        }

        /// <summary>
        /// 앱이동
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="sessionRef"></param>
        private void OnRecvInvenRequest(MessageWrapper wrapper, IActorRef sessionRef)
        {            
            var request = wrapper.InvenRequest;
            var db = GetComponent<MySqlDbComponent>();

            // tbl_inventory_accessory
            {
                var items = db.GetInvenAccessory(_userSeq);                
                var response = new MessageWrapper
                {
                    InvenAccessoryResponse = new InvenAccessoryResponse
                    {
                    }
                };                
                foreach (var item in items)
                {
                    response.InvenAccessoryResponse.Items.Add(new Item
                    {
                        CharSeq = item.char_seq,
                        ItemSeq = item.item_seq,
                        ItemType = item.item_type,
                        ItemUid = item.item_uid,
                        IsUse = item.is_use,
                        IsHold = item.is_hold,                        
                        CoupleUid = item.couple_uid,
                        ExpirationDate = Timestamp.FromDateTime(item.expiration_date),
                        Favorites = item.favorites,
                    });
                }
                Tell(response);
            }

            // tbl_inventory_set
            {
                var items = db.GetInvenSet(_userSeq);
                var response = new MessageWrapper
                {
                    InvenSetResponse = new InvenSetResponse
                    {
                    }
                };
                foreach (var item in items)
                {
                    response.InvenSetResponse.Items.Add(new Item
                    {
                        CharSeq = item.char_seq,
                        ItemSeq = item.item_seq,
                        ItemType = item.item_type,
                        ItemUid = item.item_uid,
                        IsUse = item.is_use,
                        IsHold = item.is_hold,
                        ByClass = item.byClass,
                        CoupleUid = item.couple_uid,
                        ExpirationDate = Timestamp.FromDateTime(item.expiration_date),
                        Favorites = item.favorites,
                    });
                }
                Tell(response);
            }

            // tbl_inventory_item
            {
                var items = db.GetInvenItem(_userSeq);
                var response = new MessageWrapper
                {
                    InvenItemResponse = new InvenItemResponse
                    {
                    }
                };
                foreach (var item in items)
                {
                    response.InvenItemResponse.Items.Add(new Item
                    {
                        CharSeq = item.char_seq,
                        ItemSeq = item.item_seq,
                        ItemType = item.item_type,
                        ItemUid = item.item_uid,
                        IsUse = item.is_use,
                        IsHold = item.is_hold,
                        ByClass = item.byClass,
                        CoupleUid = item.couple_uid,
                        ExpirationDate = Timestamp.FromDateTime(item.expiration_date),
                        Favorites = item.favorites,
                    });
                }
                Tell(response);
            }
        }
        private void OnRecvMapEnterResonponse(MapActor.EnterMapResponse received, IActorRef sender)
        {
            _logger.Debug($"MapActor.EnterMapResponse userUid:{received.UserUid}");
            _mapActorRef = received.MapActorRef;
        }

        /// <summary>
        /// 채팅 메시지
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="sessionRef"></param>
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
            TellMap(response);
        }


    }
}
