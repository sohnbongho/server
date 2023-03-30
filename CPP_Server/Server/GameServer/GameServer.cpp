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

	// UDP
	SOCKET serverSocket = ::socket(AF_INET, SOCK_DGRAM, 0);
	if (serverSocket == INVALID_SOCKET)
	{
		HandleError("SOcket");		
		return 0;
	}

	// 옵션을 해석하고 처리할 주체 -> 레벨
	// 소켓 코드 -> SOL_SOCKET
	// Ipv4 -> IPPROTO_IP
	// TCP 프로토콜 -> IPPROTO_TCP

	// SO_KEEPALIVE = 주기적으로 연결 상태 확인 여부(TCP only)
	// 상대방이 소리소문없이 연결 끊는다면?
	// 주기적으로 TCP 프로토콜 연결 상태 확인 -> 끊어진 연결 감지
	bool enable = true;
	::setsockopt(serverSocket, SOL_SOCKET, SO_KEEPALIVE, (char*)&enable, sizeof(enable));

	// SO_LINGER = 지연하다.
	// 송신 버퍼에 있는 데이터를 보낼 것인가? 날릴 것인가?

	// onoff =0이면 closesocket()이 바로 리턴, 아니면 linger초만큼 대기(default 0)
	// linger : 대기 시간
	LINGER linger;
	linger.l_onoff = 1;
	linger.l_linger = 5;
	::setsockopt(serverSocket, SOL_SOCKET, SO_LINGER, (char*)&linger, sizeof(linger));
	
	// Half-Close
	// SD_SEND : send 막는다.
	// SD_RECIEVE : recv 막는다.
	// SD_BOTH : 둘다 막는다.
	//::shutdown(serverSocket, SD_SEND);
	// 소켓을 닫을 때 바로 닫지 말고 shutdown으로 먼저 막고
	// 소켓 리소스 반환
	// send -> closesocket
	//::closesocket(serverSocket);

	// SO_SNDBUF = 송신 버퍼 크기
 	// SO_RCVBUF = 수신 버퍼 크기
	int32 sendBuffSize;
	int32 optionLen = sizeof(sendBuffSize);
	::getsockopt(serverSocket, SOL_SOCKET, SO_SNDBUF, (char*)&sendBuffSize, &optionLen);
	cout << "송신 버퍼 크기 : " << sendBuffSize << endl;

	int32 recvBuffSize;
	optionLen = sizeof(recvBuffSize);
	::getsockopt(serverSocket, SOL_SOCKET, SO_RCVBUF, (char*)&recvBuffSize, &optionLen);
	cout << "수신 버퍼 크기 : " << recvBuffSize << endl;

	// SO_REUSEADDR
	// IP 주소 및 port 재사용
	// 서버가 비정상 된 후, 시간을 어느정도 기다릴 상황도 있을 수 있는데
	// 강제로 재사용 하겠다고 설정
	{
		bool enable = true;
		::setsockopt(serverSocket, SOL_SOCKET, SO_REUSEADDR, (char*)&enable, sizeof(enable));		
	}

	// IPPROTO_TCP
	// TCP_NODELAY = Nagle 네이글 알고리즘 작동 여부
	// 데이터가 충분히 크면 보내고, 그렇지 않으면 데이터가 충분히 쌓였을때까지 대기하고 보낸다.
	// 장점 : 작은 패킷이 불필요하게 많이 생성되는 일을 방지
	// 단점 : 반응 시간 손해
	{
		// 게임에서는 네이글 알고리즘을 꺼준다.
		// true: 끈다
		bool noDelay = true;
		::setsockopt(serverSocket, IPPROTO_TCP, TCP_NODELAY, (char*)&noDelay, sizeof(noDelay));
	}

	// ---------------------------
	// winsocke종료
	::WSACleanup();
}
