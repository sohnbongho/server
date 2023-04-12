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

SendBufferRef ServerPacketHandler::MakeSendBuffer(Protocol::S_TEST& pkt)
{
	return _MakeSendBuffer(pkt, S_TEST);
}

