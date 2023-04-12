#include "pch.h"
#include "ClientPacketHandler.h"
#include "BufferReader.h"
#include "Protocol.pb.h"


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

void ClientPacketHandler::Handle_S_TEST(BYTE* buffer, int32 len)
{
	Protocol::S_TEST pkt;

	ASSERT_CRASH(pkt.ParseFromArray(buffer + sizeof(PacketHeader), len - sizeof(PacketHeader)));

	cout << pkt.id() << " " << pkt.hp() << " " << pkt.attack() << endl;

	cout << "BUFSIZE: " << pkt.buffs_size() << endl;

	for(auto& buf : pkt.buffs())
	{
		cout << "BuffInfo:" << buf.buffid() << " " << buf.remaintime() << endl;
		cout << "Victimes::" << buf.victims_size() << endl;
		for(auto& vic  : buf.victims())
		{
			cout << vic << " ";
		}
		cout << endl;
	}
	
}
