#include "pch.h"
#include "ClientPacketHandler.h"
#include "BufferReader.h"


void ClientPacketHandler::HandlerPacket(BYTE* buffer, int32 len)
{
	BufferReader br(buffer, len);
	PacketHeader header = *((PacketHeader*)buffer);
	br >> header;

	switch (header.id)
	{
	case S_TEST:
		Handle_S_TEST(buffer, len);
		break;
	}	
}

#pragma pack(1)
// [PKT_S_TEST][BuffData BuffData BuffData]
struct PKT_S_TEST
{
	// 패킷 설계 TEMP
	struct BuffListItem
	{
		uint64 buffId;
		float remainTime;
	};

	uint16 packetSize; // 공용헤더
	uint16 packetId; // 공용헤더
	uint64 id; // 8
	uint32 hp; // 4
	uint16 attack; // 2
	//가변 길이의 데이터
	uint16 buffOffset; //  가변데이터의 시작점
	uint16 buffCount;

	bool Validate()
	{
		uint32 size = 0;
		size += sizeof(PKT_S_TEST);
		size += buffCount * sizeof(BuffListItem);
		if (size != packetSize)
			return false;

		if ((buffOffset + buffCount * sizeof(BuffListItem)) > packetSize)
			return false;

		return true;
	}

};
#pragma pack()

void ClientPacketHandler::Handle_S_TEST(BYTE* buffer, int32 len)
{
	BufferReader br(buffer, len);

	if (len < sizeof(PKT_S_TEST))
		return;

	PKT_S_TEST pkt;
	br >> pkt;
	if (pkt.Validate() == false)
		return;

	//cout << "Id:" << id << " Hp:" << hp << " ATT:" << attack << endl;

	vector<PKT_S_TEST::BuffListItem> buffs;
	
	buffs.resize(pkt.buffCount);
	for(int32 i= 0 ;i < pkt.buffCount; i++)
		br >> buffs[i];

	cout << "BufCount:" << pkt.buffCount << endl;
	for (int32 i = 0; i < pkt.buffCount; i++)
	{
		cout << "bufInfo:" << buffs[i].buffId << " " << buffs[i].remainTime << endl;
	}
}
