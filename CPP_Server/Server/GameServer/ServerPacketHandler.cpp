#include "pch.h"
#include "ServerPacketHandler.h"

#include "BufferReader.h"
#include "BufferWriter.h"

void ServerPacketHandler::HandlerPacket(BYTE* buffer, int32 len)
{
	BufferReader br(buffer, len);

	PacketHeader header;
	br.Peek(&header);

	switch(header.id)
	{
	case S_TEST:
		PacketHeader header = *((PacketHeader*)buffer);
		cout << "Packet Id : " << header.id << "Size : " << header.size << endl;
		break;
		
	default:
		break;
	}
}

SendBufferRef ServerPacketHandler::Make_S_TEST(uint64 id, uint32 hp, uint16 attack, vector<BuffData> buffs, wstring name)
{
	SendBufferRef sendBuffer = GSendBufferManager->Open(4096);

	BufferWriter bw(sendBuffer->Buffer(), sendBuffer->AllocSize());

	PacketHeader* header = bw.Reserve<PacketHeader>();
	// id(uint64), 체력(uint32), 공격력(uint16)
	bw << id << hp << attack;

	struct ListHeader
	{
		uint16 offset;
		uint16 count;
	};

	// 가변데이터
	ListHeader* bufferHeader = bw.Reserve<ListHeader>();
	bufferHeader->offset = bw.WriteSize();
	bufferHeader->count = buffs.size();	

	for(BuffData& buff : buffs)
	{
		bw << buff.buffId << buff.remainTime;
	}	

	header->size = bw.WriteSize();
	header->id = S_TEST; // 1 : Test Msg

	sendBuffer->Close(bw.WriteSize());

	return sendBuffer;
}
