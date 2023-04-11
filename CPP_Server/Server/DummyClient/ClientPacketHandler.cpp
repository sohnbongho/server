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

		if (packetSize < size)
			return false;

		size += buffCount * sizeof(BuffListItem);
		if (size != packetSize)
			return false;

		if ((buffOffset + buffCount * sizeof(BuffListItem)) > packetSize)
			return false;

		return true;
	}

	using BuffsList = PacketList<PKT_S_TEST::BuffListItem>;
	BuffsList GetBuffsList()
	{
		BYTE* data = reinterpret_cast<BYTE*>(this);
		data += buffOffset;

		return BuffsList(reinterpret_cast<PKT_S_TEST::BuffListItem*>(data), buffCount);
	}

};
#pragma pack()

// [PKT_S_TEST][BuffData BuffData BuffData]
void ClientPacketHandler::Handle_S_TEST(BYTE* buffer, int32 len)
{
	BufferReader br(buffer, len);
	
	PKT_S_TEST* pkt = reinterpret_cast<PKT_S_TEST*>(buffer);

	if (pkt->Validate() == false)
		return;	

	//cout << "Id:" << id << " Hp:" << hp << " ATT:" << attack << endl;

	PKT_S_TEST::BuffsList buffs = pkt->GetBuffsList();	

	cout << "BufCount:" << buffs.Count() << endl;
	for (int32 i = 0; i < buffs.Count(); i++)
	{
		cout << "bufInfo1:" << buffs[i].buffId << " " << buffs[i].remainTime << endl;
	}
	for(auto it = buffs.begin(); it != buffs.end(); ++it)
	{
		cout << "bufInfo2:" << it->buffId << " " << it->remainTime << endl;
		
	}
	for (auto& buff : buffs)
	{
		cout << "bufInfo3:" << buff.buffId << " " << buff.remainTime << endl;
	}	
}
