using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Listener
    {
        Socket _listenSocket;
        Func<Session> _sessionFactory;
        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            // 문지기
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory += sessionFactory;

            // 문지기 교육
            _listenSocket.Bind(endPoint);

            // 영업 시작
            // backLog : 최대 대기수
            _listenSocket.Listen(10);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args); // 낚시대를 던진다.
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            // 기존의 있던 이벤트 삭제
            args.AcceptSocket = null;

            bool pending = _listenSocket.AcceptAsync(args);
            if (pending == false) // 바로 잡혔다.
                OnAcceptCompleted(null, args);

            // 그것이 아니면 좀있다 발생
        }


        void OnAcceptCompleted(Object sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory.Invoke();
                session.Start(args.AcceptSocket);
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);                
            }
            else
                Console.WriteLine(args.SocketError.ToString());

            RegisterAccept(args); // 낚시대를 던진다.
        }       

    }
}
