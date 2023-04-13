#include "pch.h"
#include "ServerPacketHandler.h"

PacketHandlerFunc GPacketHandler[UINT16_MAX];


bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len)
{
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
	// TODO: Log
	return true;
}

bool Handle_S_Test(PacketSessionRef& session, Protocol::S_TEST& pkt)
{
	// TODO:
	return true;
}
