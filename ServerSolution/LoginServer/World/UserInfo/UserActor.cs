using Akka.Actor;
using log4net;
using Messages;
using Newtonsoft.Json;
using System.Reflection;
using LoginServer.DataBase.MySql;
using LoginServer.DataBase.Redis;
using LoginServer.Helper;
using LoginServer.Socket;
using LoginServer.Component.DataBase;
using LoginServer.Component;
using LoginServer.World.Map;
using System.Reflection.Metadata.Ecma335;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Library.Component;
using Library.messages;

namespace LoginServer.World.UserInfo
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

        // 내가 속해있는 맵 actor
        public IActorRef _mapActorRef { get; private set; } = ActorRefs.Nobody;

        private string _remoteAddress;
        
        private ulong _userSeq = 0; // 
        private string _userId = string.Empty; // 
        

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

            // 
            // 생성과 동시에 메서드를 등록하는 dictionary
            _systemHandlers = new Dictionary<System.Type, Action<object, IActorRef>> {                                                
                {typeof(SessionActor.UserToSessionLinkResponse), (data, sender) => OnRecvUserToSessionLinkResponse((SessionActor.UserToSessionLinkResponse)data)},
            };
            
            // 세션관련 
            _sessionHandlers = new Dictionary<System.Type, Action<object, IActorRef>>{
                {typeof(UserActor.SessionReceiveData), (data, sender) => OnRecvPacket((UserActor.SessionReceiveData)data, sender)},                
            };

            // 유저 관련 핸들러
            _userHandlers = new Dictionary<MessageWrapper.PayloadOneofCase, Action<MessageWrapper, IActorRef>>
            {
                {MessageWrapper.PayloadOneofCase.LoginDirectRequest, (data, sender) => OnRecvServerLoginDirectRequest(data, sender)},
            };
            
        }        
        private void Init()
        {
            _remoteAddress = string.Empty;
            _userSeq = 0; // 
            _userId = string.Empty; //
        }

        protected override void PreStart()
        {
            base.PreStart();

            Init(); // 변수 초기화

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
                    UserSeq = _userSeq
                });
            }

            // Compone 제거
            RemoveComponent<MySqlDbComponent>();
            RemoveComponent<RedisCacheComponent>();
            _componentManager.Dispose();

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
            _logger.Debug($"OnRecvUserToSessionLinkResponse");

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
        /// 개발용 Login 에 대한 처리
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="sessionRef"></param>
        private void OnRecvServerLoginDirectRequest(MessageWrapper wrapper, IActorRef sessionRef)
        {
            var request = wrapper.LoginDirectRequest;
            //var redis = GetComponent<RedisCacheComponent>();
            var db = GetComponent<MySqlDbComponent>();

            var dummyUserList = DummyUserListHelper.Instance.DummyUserList;
            if(dummyUserList.TryGetValue(request.AccountId, out var user) == false) 
            {
                // ID가 없다.
                // 클라이언트에게 알림
                var response = new MessageWrapper
                {
                    LoginDirectResponse = new LoginDirectResponse
                    {
                        Result = 0,
                        UserSeq = _userSeq,
                    }
                };
                Tell(response);
                return;
            }

            _userSeq = user.UserSeq;
            _userId = user.Account;
            
            var serverList = db.GetServerList(ServerType.Lobby);

            // 클라이언트에게 알림
            {
                var response = new MessageWrapper
                {
                    LoginDirectResponse = new LoginDirectResponse
                    {
                        Result = 1,
                        UserSeq = _userSeq,
                    }
                };
                foreach (var server in serverList)
                {
                    response.LoginDirectResponse.LobbyInfos.Add(new LobbyInfo
                    {
                        Id = server.server_id,
                        WorldIdx = server.world_id,
                        Ip = server.ipaddr,                        
                        Name = server.server_name,
                        Port = server.port,
                    });
                }

                Tell(response);
            }            
        }
    }
}
