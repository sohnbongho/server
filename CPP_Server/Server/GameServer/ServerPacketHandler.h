#pragma once

enum
{
	S_TEST = 1
};

// 패킷 설계 TEMP
struct BuffData
{
	uint64 buffId;
	float remainTime;

};

class ServerPacketHandler
{
public:
	static void HandlerPacket(BYTE* buffer, int32 len);

	static SendBufferRef Make_S_TEST(uint64 id, uint32 hp, uint16 attack, vector<BuffData> buffs, wstring name);
	
};
