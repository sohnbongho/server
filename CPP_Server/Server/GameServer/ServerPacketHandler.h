#pragma once
#include "BufferWriter.h"

enum
{
	S_TEST = 1
};
template<typename T, typename C>
class PacketIterator
{
public:
	PacketIterator(C& container, uint16 index) : _container(container), _index(index) {}

	bool operator !=(const PacketIterator& other) const { return other._index != _index; }
	const T& operator*() const { return _container[_index]; }
	T& operator*() { return _container[_index]; }
	T* operator->() const { return &_container[_index]; }
	PacketIterator& operator++() { _index++; return *this; }
	PacketIterator operator++(int32) {
		PacketIterator ret = *this; ++_index; return ret;
	}

private:
	C& _container;
	uint16			_index;
};

template<typename T>
class PacketList
{
public:
	PacketList() : _data(nullptr), _count(0)
	{

	}
	PacketList(T* data, uint16 cout) : _data(data), _count(cout)
	{

	}
	T& operator[](uint16 index)
	{
		ASSERT_CRASH(index < _count);
		return _data[index];
	}

	uint16 Count() { return _count; }

	// ranged-base for 지원
	PacketIterator<T, PacketList<T>> begin() { return PacketIterator<T, PacketList<T >>(*this, 0); }
	PacketIterator<T, PacketList<T>> end() { return PacketIterator<T, PacketList<T >>(*this, _count); }

private:
	T* _data;
	uint16			_count;

};


class ServerPacketHandler
{
public:
	static void HandlerPacket(BYTE* buffer, int32 len);

};

#pragma pack(1)
// [PKT_S_TEST][BuffData BuffData BuffData]
struct PKT_S_TEST
{
	// 패킷 설계 TEMP
	struct BuffsListItem
	{
		uint64 buffId;
		float remainTime;

		// Victim List
		uint16 victiomOffset;
		uint16 victiomCount;
	};

	uint16 packetSize; // 공용헤더
	uint16 packetId; // 공용헤더
	uint64 id; // 8
	uint32 hp; // 4
	uint16 attack; // 2
	//가변 길이의 데이터
	uint16 buffOffset; //  가변데이터의 시작점
	uint16 buffCount;	

	

};

// [PKT_S_TEST][BuffData BuffData BuffData][victim victim]
class PKT_S_TEST_WRITE
{
public:
	using BuffsListItem = PKT_S_TEST::BuffsListItem;
	using BuffsList = PacketList<PKT_S_TEST::BuffsListItem>;
	using BuffVictimsList = PacketList<uint64>;

	PKT_S_TEST_WRITE(uint64 id, uint32 hp, uint16 attack)
	{
		_sendBuffer = GSendBufferManager->Open(4096);
		_bw = BufferWriter(_sendBuffer->Buffer(), _sendBuffer->AllocSize());

		_pkt = _bw.Reserve<PKT_S_TEST>();
		_pkt->packetSize = 0; // To Fill
		_pkt->packetId = S_TEST;
		_pkt->id = id;
		_pkt->hp = hp;
		_pkt->attack = attack;
		_pkt->buffOffset = 0; // To Fill
		_pkt->buffCount = 0; // To Fill
	}

	BuffsList ReserveBuffList(uint16 buffCount)
	{
		BuffsListItem* firstBuffListItem = _bw.Reserve<BuffsListItem>(buffCount);
		_pkt->buffOffset = (uint64)firstBuffListItem - (uint64)_pkt;
		_pkt->buffCount = buffCount;
		return BuffsList(firstBuffListItem, buffCount);		
	}

	BuffVictimsList ReserveBuffVictimsList(BuffsListItem* buffsItem, uint16 victimsCount)
	{
		uint64* firstVictimsListItem = _bw.Reserve<uint64>(victimsCount);
		buffsItem->victiomOffset = (uint64)firstVictimsListItem - (uint64)_pkt;
		buffsItem->victiomCount = victimsCount;
		return BuffVictimsList(firstVictimsListItem, victimsCount);
	}

	SendBufferRef CloseAndReturn()
	{
		// 패킷 사이즈 계산
		_pkt->packetSize = _bw.WriteSize();

		_sendBuffer->Close(_bw.WriteSize());
		return _sendBuffer;

	}

private:
	PKT_S_TEST* _pkt = nullptr;
	SendBufferRef _sendBuffer;
	BufferWriter _bw;

};

#pragma pack()