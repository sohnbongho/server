#include "pch.h"
#include <iostream>
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
	
	SOCKET clientSocket = ::socket(AF_INET, SOCK_DGRAM, 0);
	if(clientSocket == INVALID_SOCKET)
	{
		HandleError("Socket");
		return 0;
	}
	
	SOCKADDR_IN serverAddr; // IPv4
	::memset(&serverAddr, 0,sizeof(serverAddr));
	serverAddr.sin_family = AF_INET;	
	::inet_pton(AF_INET, "127.0.0.1", &serverAddr.sin_addr);
	serverAddr.sin_port = ::htons(7777);

	// Connectd UDP
	::connect(clientSocket, (SOCKADDR*)&serverAddr, sizeof(serverAddr));
	
	while (true)
	{
		char sendBuff[100] = "Hello Server!";

		// 나의 IP주소 + 포트 번호 설정
		// 나의 주소 bind

		// unConnected UDP
		/*int32 resultCode = ::sendto(clientSocket, sendBuff, sizeof(sendBuff), 0, (SOCKADDR*)&serverAddr, sizeof(serverAddr));*/
		// Connected UDP
		int32 resultCode = ::send(clientSocket, sendBuff, sizeof(sendBuff), 0);

		if (resultCode == SOCKET_ERROR)
		{
			HandleError("Send");
			return 0;
		}
		cout << "Send Buffer Len = " << sizeof(sendBuff) << endl;

		SOCKADDR_IN recvAddr; // IPv4
		::memset(&recvAddr, 0, sizeof(recvAddr));
		int addrLen = sizeof(recvAddr);

		char recvBuffer[1000];

		// UnConnectd UDP
		/*int32 recvLen = ::recvfrom(clientSocket, recvBuffer, sizeof(recvBuffer), 0, (SOCKADDR*)&recvAddr, &addrLen);*/
		// ConnectUDP
		int32 recvLen = ::recv(clientSocket, recvBuffer, sizeof(recvBuffer), 0);

		if (recvLen <= 0)
		{
			HandleError("recvFrom");
			return 0;
		}

		cout << "Recv Data! = " << recvBuffer << endl;
		cout << "Recv Len! = " << recvLen << endl;
		
		this_thread::sleep_for(1s);
	}
	//----------------------

	// 소켓 리소스 반환
	::closesocket(clientSocket);

	// winsocke종료
	::WSACleanup();
}

