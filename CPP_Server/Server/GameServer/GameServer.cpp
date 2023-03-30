#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include "CoreMacro.h"
#include "ThreadManager.h"

#include <WinSock2.h>
#include <MSWSock.h>
#include <WS2tcpip.h>
#pragma comment(lib, "ws2_32.lib")

int main()
{
	// 윈속 초기화(ws2_32 라이브러리 초기화)
	// 관련 정보가 wsaData에 채워짐
	WSADATA wsaData;
	if (::WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
		return 0;

	// ad: Address Family (AF_INET = IPv4, AF_INET6 = IPv6)
	// type: TCP(SOCK_STREAM) vs UDP(SOCK_DGRAM)
	// protocol : 0
	// return : descriptor
	SOCKET listenSocket = ::socket(AF_INET, SOCK_STREAM, 0);
	if (listenSocket == INVALID_SOCKET)
	{
		int32 errorCode = ::WSAGetLastError();
		cout << "Soicket ErrorCode : " << errorCode << endl;
		return 0;
	}

	// 나의 주소는? (IP주소 : Port주소)-> XX 아파트 YY호
	SOCKADDR_IN serverAddr; // IPv4
	::memset(&serverAddr, 0, sizeof(serverAddr));
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_addr.s_addr = ::htonl(INADDR_ANY); 	// 니가 알아서 해줘
	serverAddr.sin_port = ::htons(7777); // 80 : HTTP

	// 안내원의 폰 개통! 식당의 대표 번호
	if(::bind(listenSocket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
	{
		int32 errorCode = ::WSAGetLastError();
		cout << "Bind ErrorCode : " << errorCode << endl;
		return 0;
	}

	// 영업 시작
	if(::listen(listenSocket, 10) == SOCKET_ERROR) // backLog: 대기열의 최대 한도
	{
		int32 errorCode = ::WSAGetLastError();
		cout << "listen ErrorCode : " << errorCode << endl;
		return 0;
	}

	// ---------------------------
	while (true)
	{
		// listenSocket: 문지기
		// clientSocket: 패킷을 주고 받을수 있는 소켓

		SOCKADDR_IN clientAddr;
		::memset(&clientAddr, 0, sizeof(clientAddr));
		int32 addrLen = sizeof(clientAddr);

		// 대리인이 들고 있는 단말기 
		SOCKET clientSocket = ::accept(listenSocket, (SOCKADDR*)&clientAddr, &addrLen);

		if(clientSocket == INVALID_SOCKET)
		{
			int32 errorCode = ::WSAGetLastError();
			cout << "accept ErrorCode : " << errorCode << endl;
			return 0;
		}

		// 손님 입장!
		char ipAddress[16];
		::inet_ntop(AF_INET, &clientAddr.sin_addr, ipAddress, sizeof(ipAddress));
		cout << "Client Connected Ip = " << ipAddress << endl;

		while(true)
		{
			char recvBuffer[1000];

			this_thread::sleep_for(1s);

			int32 recvLen = ::recv(clientSocket, recvBuffer, sizeof(recvBuffer), 0);
			if(recvLen <= 0)
			{
				int32 errorCode = ::WSAGetLastError();
				cout << "recv ErrorCode : " << errorCode << endl;
				return 0;
			}
			cout << "Recv Data! = " << recvBuffer << endl;
			cout << "Recv Len! = " << recvLen << endl;

			/*int32 resultCode = ::send(clientSocket, recvBuffer, recvLen, 0);

			if (resultCode == SOCKET_ERROR)
			{
				int32 errCode = ::WSAGetLastError();
				cout << "Send ErrorCode : " << errCode << endl;
				return 0;
			}*/



		}
	}

	// ---------------------------
	// winsocke종료
	::WSACleanup();
}
