#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include "CoreMacro.h"
#include "ThreadManager.h"

#include <WinSock2.h>
#include <MSWSock.h>
#include <WS2tcpip.h>
#pragma comment(lib, "ws2_32.lib")

void HandleError(const char* cause)
{
	int32 errorCode = ::GetLastError();
	cout << cause << " Error Cocde : " << errorCode << endl;
}

int main()
{	
	WSADATA wsaData;
	if (::WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
		return 0;

	// 블록킹(Blocking)
	// aceept -> 접속한 클라가 있을 때,
	// connect-> 서버 접속 성공했을 때
	// send, sendto -> 요청한 데이터를 송신 버퍼에 복사 했을 때
	// recv, recvto-> 수신 버퍼에 도착한 데이터가 있고, 이를 유저 레벨 버퍼에 복사했을 때

	// 논블록킹(Non-Blocking)
	SOCKET listenSocket = ::socket(AF_INET, SOCK_STREAM, 0);
	if (listenSocket == INVALID_SOCKET)
		return 0;

	// 논블러킹 소켓 생성
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

	cout << "Accept " << endl;
	SOCKADDR_IN clientAddr;
	int32 addrLen = sizeof(clientAddr);

	// Accept
	while(true)
	{
		SOCKET clientSocket = ::accept(listenSocket, (SOCKADDR*)&clientAddr, &addrLen);
		if (clientSocket == INVALID_SOCKET)
		{
			// 논블러킹에서는 문제상황이 아니다.
			// 원래 블록했어야 했는데......
			if (::WSAGetLastError() == WSAEWOULDBLOCK)
				continue;

			// ERROR
			break;
		}
		cout << "Client Connected!" << endl;

		// Recv
		while(true)
		{
			char recvBuffer[1000];
			int32 recvLen = ::recv(clientSocket, recvBuffer, sizeof(recvBuffer), 0);
			if(recvLen == SOCKET_ERROR)
			{
				if(::WSAGetLastError() == WSAEWOULDBLOCK)
					continue;

				// ERROR
				break;
			}
			else if (recvLen == 0)
			{
				// 연결 끊김.
				break;
			}

			cout << "Recv Data Len = " << recvLen << endl;

			// Send
			while(true)
			{
				if(::send(clientSocket, recvBuffer, recvLen, 0 ) == SOCKET_ERROR)
				{
					// 원래 블록했어야 했는데...
					if(::WSAGetLastError() == WSAEWOULDBLOCK)
						continue;

					// ERROR
					break;
				}
				cout << "Send Data Len = " << recvLen << endl;

			}
		}
		
	}
	


	
	// ---------------------------
	// winsocke종료
	::WSACleanup();
}
