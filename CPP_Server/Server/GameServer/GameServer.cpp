#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <atomic>
#include <mutex>
#include <windows.h>
#include <future>
#include "ThreadManager.h"

#include <winsock2.h>
#include <mswsock.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")

void HandleError(const char* cause)
{
	int32 errCode = ::WSAGetLastError();
	cout << cause << " ErrorCode : " << errCode << endl;
}

const int32 BUFSIZE = 1000;

struct Session
{
	WSAOVERLAPPED ovrelapped = {};
	SOCKET socket = INVALID_SOCKET;
	char recvBuffer[BUFSIZE] = {};
	int32 recvBytes = 0;	
};
// 1)오류 발생시 0 아닌값
// 2) 전송 바이트 수
// 3) 비동기 입출력 함수 호출 시 넘겨준 WSAOVERLAPPED 구조체의 주소값
// 4) 0
void CALLBACK RecvCallback(DWORD error, DWORD recvLen, LPWSAOVERLAPPED overlapped, DWORD floags)
{
	cout << "Data Recv Len Callback = " << recvLen << endl;
	// TODO: 에코 서버를 만들다면 WSASend()

	Session* session = (Session*)overlapped;
}

int main()
{
	WSAData wsaData;
	if (::WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
		return 0;

	SOCKET listenSocket = ::socket(AF_INET, SOCK_STREAM, 0);
	if (listenSocket == INVALID_SOCKET)
		return 0;

	u_long on = 1;
	if (::ioctlsocket(listenSocket, FIONBIO, &on) == INVALID_SOCKET)
		return 0;

	SOCKADDR_IN serverAddr;
	::memset(&serverAddr, 0, sizeof(serverAddr));
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_addr.s_addr = ::htonl(INADDR_ANY);
	serverAddr.sin_port = ::htons(7777);

	if (::bind(listenSocket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
		return 0;

	if (::listen(listenSocket, SOMAXCONN) == SOCKET_ERROR)
		return 0;

	cout << "Accept" << endl;
	
	// Overlapped 모델(Completion Routine 콜백 기반)
	// - 비동기 입출력 지원하는 소켓 생성
	// - 비동기 입출력 함수 호출 (완료 루틴의 시작 주소를 넘겨준다.)
	// - 비동기 작업이 바로 완료되지 않으면, WSA_IO_PENDING 오류 코드
	// - 비동기 입출력 함수 호출한 쓰레드를 -> Alertable Wait상태로 만든다.
	// ex) WaitForSingObject, WaitForMultipleObject, SleepEx, WSAWaitMultipleEvent
	// - 비동기 IO 완료되면, 운영체제는 완료 루틴 호출
	// - 완료 루틴 호출이 모두 끝나면, 쓰레드는 Alertable Wait상태에서 빠져나온다.	

	// 1)오류 발생시 0 아닌값
	// 2) 전송 바이트 수
	// 3) 비동기 입출력 함수 호출 시 넘겨준 WSAOVERLAPPED 구조체의 주소값
	// 4) 0
	// void CompletionRoutine()

	// Select 모델
	// - 장점) 윈도우/리눅스 공통
	// - 단점) 성능 최하 (매번 등록 비용), 64개 제한
	// WSAAsyncSelect 모델 = 소켓 이벤트를 윈도우 메시지 형태로 처리.(일반 윈도우 메시지랑 같이 처리하니 성능이 .....)
	// WSAEventSelect 모델
	// - 장점) 비교적 뛰어난 성능
	// - 단점) 64개 제한
	// Overlapped (이벤트 기반)
	// - 장점) 성능
	// - 단점) 64개 제한
	// Overlapped (콜백 기반)
	// - 장점) 성능
	// - 단점) 모든 비동기 함수에서 사용 가능하진 않음(accept),
	//			빈번한 alertable wait으로 인한 성능 저하
	// IOCP
	// - 네트워크 라이브러리에 들어갈 끝판왕

	// Reactor Pattern (-뒤늦게, 논블러킹 소켓, 소켓 상태 확인 후 -> 뒤늦게 recv, send 호출)
	// Procator Pattern (-미리, Overlapped WSA~) 

	while(true)
	{
		SOCKADDR_IN clientAddr;
		int32 addrLen = sizeof(clientAddr);

		SOCKET clientSocket;
		while(true)
		{
			clientSocket = ::accept(listenSocket, (SOCKADDR*)&clientAddr, &addrLen);
			if (clientSocket != INVALID_SOCKET)
				break;
			if(::WSAGetLastError() == WSAEWOULDBLOCK)
				continue;

			//  문제 있는 상황
			return 0;			
		}

		Session session = Session{ clientSocket };
		cout << "Client Connected !" << endl;

		while(true)
		{
			WSABUF wsaBuf;
			wsaBuf.buf = session.recvBuffer;
			wsaBuf.len = BUFSIZE;

			DWORD recvLen = 0;
			DWORD flags = 0;
			if(::WSARecv(clientSocket, &wsaBuf, 1, &recvLen, &flags, &session.ovrelapped, RecvCallback) == SOCKET_ERROR)
			{
				if(::WSAGetLastError() == WSA_IO_PENDING)
				{					
					// Pending
					// Alertable Wait 상태로 (데이터 올떄까지 대기 상태로)
					//::WSAWaitForMultipleEvents(1, &wsaEvent, TRUE, WSA_INFINITE, TRUE);
					::SleepEx(INFINITE, TRUE);
				}
				else
				{
					// TODO: 문제 있는 상황
					break;
				}
			}
			else
			{
				cout << "Data Recv Len" << recvLen << endl;
			}
		}

		::closesocket(session.socket);		
	}

	// 윈속 종료
	::WSACleanup();
}