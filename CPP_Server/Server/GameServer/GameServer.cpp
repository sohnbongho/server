#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <atomic>
#include <mutex>
#include <windows.h>
#include <future>
#include "ThreadManager.h"
#include "Memory.h"

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
	SOCKET socket = INVALID_SOCKET;
	char recvBuffer[BUFSIZE] = {};
	int32 recvBytes = 0;	
};

enum IO_TYPE
{
	READ,
	WRITE,
	ACCEPT,
	CONNECT,
};

struct OverlappedEx
{
	WSAOVERLAPPED ovrelapped = {};
	int32 type = 0; // read, wirate, accept, connect.....
	
};

void WorkerThreadMain(HANDLE iocpHandle)
{
	while(true)
	{
		DWORD byteTransferred = 0;
		Session* session = nullptr;
		OverlappedEx* overlappedEx = nullptr;

		// 데이터가 있는지 계속 대기
		BOOL ret = ::GetQueuedCompletionStatus(iocpHandle, &byteTransferred, (ULONG_PTR*)&session, (LPOVERLAPPED*)&overlappedEx, INFINITE);

		if(ret == FALSE || byteTransferred == 0)
		{
			// TODO: 연결 끊김
			continue;
		}

		ASSERT_CRASH(overlappedEx->type == IO_TYPE::READ);

		cout << "Recv Data IOCP = " << byteTransferred << endl;

		// Callback에서 recv를 다시 시도한다.
		WSABUF wsaBuf;
		wsaBuf.buf = session->recvBuffer;
		wsaBuf.len = BUFSIZE;

		DWORD recvLen = 0;
		DWORD flags = 0;
		::WSARecv(session->socket, &wsaBuf, 1, &recvLen, &flags, 
			&overlappedEx->ovrelapped, NULL);
	}
}

int main()
{
	WSAData wsaData;
	if (::WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
		return 0;

	SOCKET listenSocket = ::socket(AF_INET, SOCK_STREAM, 0);
	if (listenSocket == INVALID_SOCKET)
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
	// - 비동기 입출력 함수 완료되면 쓰레드마다 있는 APC큐에 일감이 쌓임
	// - AlertWait 상태에 들어가서 APC큐 비우기 (콜백함수)
	// 단점) APC큐 쓰레드마다 있다!
	// 단점) 이벤트 방식 소켓: 이벤트 1:1 대응

	// ICCP (Completion Port)모델
	// - APC-> Completion Port(쓰레드 마다 있는건 아니고 1개, 중앙에서 관리하는 APC큐)
	// - Aleratable Wait -> CP 결과 처리를 GetQueuedCompletionStatus
	// 쓰레드랑 궁합이 굉장히 좋다!

	// CreateIoCompetionPort
	// GetQueuedCompletionStatus

	vector<Session*> sessionManager;

	// CP 생성
	HANDLE iocpHandle = ::CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);

	// WorkderThreads
	for(int32 i =0 ;i < 5; i++)
	{
		GThreadManager->Launch([=]()
			{
				WorkerThreadMain(iocpHandle);
			});
	}

	// Main Thread = Accept  담당
	while(true)
	{
		SOCKADDR_IN clientAddr;
		int32 addrLen = sizeof(clientAddr);

		SOCKET clientSocket = ::accept(listenSocket, (SOCKADDR*)&clientAddr, &addrLen);
		if (clientSocket == INVALID_SOCKET)
		{
			return 0;
		}

		// 
		//Session* session = new Session{ clientSocket };
		Session* session = xnew<Session>();
		session->socket = clientSocket;
		sessionManager.push_back(session);		
		
		cout << "Client Connected !" << endl;
		 
		// 소켓을 CP에 등록
		::CreateIoCompletionPort((HANDLE)clientSocket, iocpHandle, /*key*/(ULONG_PTR)session, 0);

		WSABUF wsaBuf;
		wsaBuf.buf = session->recvBuffer;
		wsaBuf.len = BUFSIZE;

		OverlappedEx* overlappedEx = new OverlappedEx();
		overlappedEx->type = IO_TYPE::READ;

		// 레퍼런스를 1증가하는 방법으로 메모리 문제를 해결해야!!!
		DWORD recvLen = 0;
		DWORD flags = 0;
		::WSARecv(clientSocket, &wsaBuf, 1, &recvLen, &flags, &overlappedEx->ovrelapped, NULL);

		// 유저가 게임 접속 종료!
		// 메모리 오류의 문제 발생!!!
		/*Session* s = sessionManager.back();
		sessionManager.pop_back();
		xdelete(s);*/

	}
	GThreadManager->Join();

	// 윈속 종료
	::WSACleanup();
}