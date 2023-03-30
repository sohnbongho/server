#include "pch.h"
#include <iostream>
#include <WinSock2.h>
#include <MSWSock.h>
#include <WS2tcpip.h>
#pragma comment(lib, "ws2_32.lib")

int main()
{		
	WSADATA wsaData;
	if (::WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
		return 0;
	
	SOCKET clientSocket = ::socket(AF_INET, SOCK_STREAM, 0);
	if(clientSocket == INVALID_SOCKET)
	{
		int32 errorCode = ::WSAGetLastError();
		cout << "Soicket ErrorCode : " << errorCode << endl;
		return 0;
	}

	
	SOCKADDR_IN serverAddr; // IPv4
	::memset(&serverAddr, 0,sizeof(serverAddr));
	serverAddr.sin_family = AF_INET;	
	::inet_pton(AF_INET, "127.0.0.1", &serverAddr.sin_addr);
	serverAddr.sin_port = ::htons(7777); // 80 : HTTP
	
	if(::connect(clientSocket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) ==SOCKET_ERROR)
	{
		int32 errorCode = ::WSAGetLastError();
		cout << "Connect ErrorCode : " << errorCode << endl;
		return 0;
	}

	//--------------------
	// 연결 성공: 이제부터 데이터 송수신 가능!
	cout << "Connected To Server !" << endl;

	while (true)
	{
		char sendBuff[100] = "Hello Server!";

		int32 resultCode = ::send(clientSocket, sendBuff, sizeof(sendBuff), 0);

		if(resultCode == SOCKET_ERROR)
		{
			int32 errCode = ::WSAGetLastError();
			cout << "Send ErrorCode : " << errCode << endl;
			return 0;			
		}
		
		this_thread::sleep_for(1s);
	}
	//----------------------

	// 소켓 리소스 반환
	::closesocket(clientSocket);

	// winsocke종료
	::WSACleanup();
}

