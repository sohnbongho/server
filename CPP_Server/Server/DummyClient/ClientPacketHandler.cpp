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
// [PKT_S_TEST][BuffData BuffData BuffData][victim victim]
struct PKT_S_TEST
{
	// ��Ŷ ���� TEMP
	struct BuffsListItem
	{
		uint64 buffId;
		float remainTime;

		uint16 victimsOffset;
		uint16 victimsCount;

		bool Validate(BYTE* packetStart, uint16 packetSize , OUT uint32& size)
		{
			if ((victimsOffset + victimsCount * sizeof(uint64)) > packetSize)
				return false;

			size += victimsCount * sizeof(uint64);
			return true;
		}
	};

	uint16 packetSize; // �������
	uint16 packetId; // �������
	uint64 id; // 8
	uint32 hp; // 4
	uint16 attack; // 2
	//���� ������ ������
	uint16 buffOffset; //  ������������ ������
	uint16 buffCount;

	bool Validate()
	{
		uint32 size = 0;
		size += sizeof(PKT_S_TEST);

		if (packetSize < size)
			return false;

		if ((buffOffset + buffCount * sizeof(BuffsListItem)) > packetSize)
			return false;

		// Buffers ���� ������ ũ�� �߰�
		size += buffCount * sizeof(BuffsListItem);

		BuffsList buffList = GetBuffsList();
		for(int32 i = 0; i < buffList.Count(); ++i)
		{
			if (buffList[i].Validate((BYTE*)this, packetSize, OUT size) == false)
				return false;
		}


		// ���� ũ�� ��
		if (size != packetSize)
			return false;		

		return true;
	}

	using BuffsList = PacketList<PKT_S_TEST::BuffsListItem>;
	using BuffsVictimsList = PacketList<uint64>;

	BuffsList GetBuffsList()
	{
		BYTE* data = reinterpret_cast<BYTE*>(this);
		data += buffOffset;

		return BuffsList(reinterpret_cast<PKT_S_TEST::BuffsListItem*>(data), buffCount);
	}

	BuffsVictimsList GetBuffsVictimsList(BuffsListItem* buffsItem)
	{
		BYTE* data = reinterpret_cast<BYTE*>(this);
		data += buffsItem->victimsOffset;

		return BuffsVictimsList(reinterpret_cast<uint64*>(data), buffsItem->victimsCount);
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
	
	for (auto& buff : buffs)
	{
		cout << "bufInfo3:" << buff.buffId << " " << buff.remainTime << endl;

		PKT_S_TEST::BuffsVictimsList victims = pkt->GetBuffsVictimsList(&buff);

		cout << "Victim Count:" << victims.Count() << endl;
		for(auto& victim : victims)
		{
			cout << "Victim:" << victim << endl;
		}
	}	
}
