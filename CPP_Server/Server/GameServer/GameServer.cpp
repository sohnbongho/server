#include "pch.h"
#include <iostream>
#include "ThreadManager.h"
#include "Service.h"
#include "GameSession.h"
#include "GameSessionManager.h"
#include "BufferWriter.h"
#include "ServerPacketHandler.h"
#include "tchar.h"


int main()
{
	// [                               ]

	PKT_S_TEST pkt;
	pkt.hp = 1;
	pkt.id = 2;
	pkt.attack = 3;


	ServerServiceRef service = MakeShared<ServerService>(
		NetAddress(L"127.0.0.1", 7777), 
		MakeShared<IocpCore>(),
		MakeShared<GameSession>, // TODO: SessionManager 등
		100 );

	ASSERT_CRASH(service->Start());

	for(int32 i =0 ;i < 5; i++)
	{
		GThreadManager->Launch([=]()
			{
				while (true)
				{
					service->GetIocpCore()->DisPatch();
				}
			});
	}
		
	WCHAR sendData3[1000] = L"가"; // UTF16 = Unicode(한글/로마 2바이트)	

	while(true)
	{
		PKT_S_TEST_WRITE pktWriter(1001, 100, 10);

		// [PKT_S_TEST][BuffData BuffData BuffData][victim victim]
		PKT_S_TEST_WRITE::BuffsList buffList = pktWriter.ReserveBuffList(3);
		buffList[0] = { 100, 1.5f };
		buffList[1] = { 200, 2.3f };
		buffList[2] = { 300, 0.7f };

		PKT_S_TEST_WRITE::BuffVictimsList vic0 = 
		pktWriter.ReserveBuffVictimsList(&buffList[0], 3);
		{
			vic0[0] = 1000;
			vic0[1] = 2000;
			vic0[2] = 3000;
		}

		PKT_S_TEST_WRITE::BuffVictimsList vic1 =
			pktWriter.ReserveBuffVictimsList(&buffList[1], 1);
		{
			vic1[0] = 4000;
		}

		PKT_S_TEST_WRITE::BuffVictimsList vic2 =
			pktWriter.ReserveBuffVictimsList(&buffList[2], 2);
		{
			vic2[0] = 3000;
			vic2[1] = 5000;
		}
		

		SendBufferRef sendBuffer = pktWriter.CloseAndReturn();
		GSessionManager.Broadcast(sendBuffer); // Braodcast

		this_thread::sleep_for(250ms);
	}	
	
	GThreadManager->Join();

}
