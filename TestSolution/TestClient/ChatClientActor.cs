using System;

using Akka.Actor;

namespace TestClient
{
    /// <summary>
    /// 채팅 클라이언트 액터
    /// </summary>
    public class ChatClientActor : ReceiveActor, ILogReceive
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Field
        ////////////////////////////////////////////////////////////////////////////////////////// Private        

        /// <summary>
        /// 액터 선택
        /// </summary>
        private const string ServerRootPath = "akka.tcp://TestServer@localhost:9999/user";
        private readonly ActorSelection actorSelection = Context.ActorSelection("akka.tcp://TestServer@localhost:9999/user/world");

        private ActorSelection WorldActorSelection(string relaitvePath) {
            return Context.ActorSelection($"{ServerRootPath}{relaitvePath}");
        }

        /// <summary>
        /// 닉네임
        /// </summary>
        private string _nickName = $"Roggan";


        /// <summary>
        /// 생성자
        /// </summary>
        public ChatClientActor(string userName)
        {
            _nickName = userName;

            Receive<Terminated>(terminated =>
            {
                // 연결 끊김 이벤트 처리 로직을 여기에 작성합니다.
                // 예를 들어, 액터를 다시 시작하거나 연결 상태를 업데이트하는 등의 작업을 수행할 수 있습니다.
                Console.WriteLine($"Remote actor terminated: {terminated.ActorRef.Path}");
            });
        }
        protected override void PostStop()
        {
            // 생존성 모니터링 종료
            Console.WriteLine("PostStop()");
            
            base.PostStop();
        }
        
    }
}