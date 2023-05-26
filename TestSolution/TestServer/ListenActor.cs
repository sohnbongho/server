using System;
using Akka.Actor;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TestLibrary;
using System.Collections.Generic;
using System.Threading;
using log4net;
using System.Reflection;

namespace TestServer
{
    /// <summary>
    /// 채팅 서버 액터
    /// </summary>
    //public class ListenActor : ReceiveActor, ILogReceive
    public class ListenerActor : UntypedActor
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Field
        ////////////////////////////////////////////////////////////////////////////////////////// Private
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 액터 참조 해시 세트
        /// </summary>

        // 원격에 연결된 User Acotr들
        private readonly ConcurrentDictionary<string, IActorRef> _remoteActorRefList = new ConcurrentDictionary<string, IActorRef>();
        private static long _userUid = 0;

        //////////////////////////////////////////////////////////////////////////////////////////////////// Constructor
        ////////////////////////////////////////////////////////////////////////////////////////// Public


        /// <summary>
        /// 생성자
        /// </summary>
        public ListenerActor()
        {
            //Receive<Terminated>(message =>
            //{
            //    // 연결 끊김 이벤트 처리 로직을 여기에 작성합니다.
            //    // 예를 들어, 액터를 다시 시작하거나 연결 상태를 업데이트하는 등의 작업을 수행할 수 있습니다.
            //    Console.WriteLine($"Terminated");
            //});

            //Receive<ConnectRequest> (   
            //    connectRequest =>{
            //        OnRecvConnectRequest(connectRequest);
            //    }
            //);

            //Receive<NickNameRequest> (
            //    nickNameRequest => {
            //        OnRecvNickNameRequest(nickNameRequest);
            //    }
            //);

            //Receive<SayRequest> (
            //    sayRequest => {
            //        OnRecvSayRequest(sayRequest);                    
            //    }
            //);

            //ReceiveAny(value =>
            //{
            //    OnRecvAny(value);
            //});
        }
        protected override void OnReceive(object message)
        {
            int kk = 0;
        }

        /// <summary>
        /// Recv C
        /// </summary>
        /// <param name="ConnectRequest"></param>
        void OnRecvConnectRequest(ConnectRequest connectRequest)
        {
            var userUid = Interlocked.Increment(ref _userUid);
            _remoteActorRefList.TryAdd(userUid.ToString(), Sender);

            Sender.Tell(new ConnectResponse{
                Message = "Hello and welcome to Akka.NET chat example",}, Self);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ConnectRequest"></param>
        void OnRecvNickNameRequest(NickNameRequest nickNameRequest)
        {
            NickNameResponse nickNameResponse = new NickNameResponse
            {
                UserName = nickNameRequest.UserName,
                NewUserName = nickNameRequest.NewUserName,
            };

            foreach (IActorRef actorRef in this._remoteActorRefList.Values)
            {
                actorRef.Tell(nickNameResponse, Self);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sayRequest"></param>
        void OnRecvSayRequest(SayRequest sayRequest)
        {
            SayResponse sayResponse = new SayResponse
            {
                UserName = sayRequest.UserName,
                Message = sayRequest.Message,
            };

            foreach (IActorRef actorRef in this._remoteActorRefList.Values)
            {
                actorRef.Tell(sayResponse, Self);
            }

        }
        private void OnRecvAny(object value)
        {
            try
            {
                Console.WriteLine($"OnRecvAny:{value.ToString()}");
            }
            catch(Exception e)
            {
                _logger.Error(e);
            }
        }

        // here we are overriding the default SupervisorStrategy
        // which is a One-For-One strategy w/ a Restart directive
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, // maxNumberOfRetries
                TimeSpan.FromSeconds(30), // duration
                x =>
                {
                    //Maybe we consider ArithmeticException to not be application critical
                    //so we just ignore the error and keep going.
                    if (x is ArithmeticException) return Directive.Resume;

                    //Error that we cannot recover from, stop the failing actor
                    else if (x is NotSupportedException) return Directive.Stop;

                    //In all other cases, just restart the failing actor
                    else return Directive.Restart;
                });
        }
    }
}