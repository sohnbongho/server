#pragma once

/*
 * 다양한 크기를 담을수 있는 풀들
 * 아래와 같이 2가지 방법이 존재
 * 1) 여러 크기 반환
 *		[32][64][][][][][][][]
 * 2) 동일한 크기 끼리 반환
 *		[32 32 32 32 32 32]
 */

/************
 *MemeryHeadr
 ************/

struct MemoryHeader
{
	// [MemoryHeader][Data]
	MemoryHeader(int32 size) : allocSize(size)
	{
		
	}
	static void* AttachHeader(MemoryHeader* header, int32 size)
	{
		new(header)MemoryHeader(size);// placement New
		return reinterpret_cast<void*>(++header); // 데이터 시작위치
	}
	static MemoryHeader* DetachHeader(void* ptr)
	{
		MemoryHeader* header = reinterpret_cast<MemoryHeader*>(ptr) - 1;
		return header;
	}	

	int32 allocSize;
	// TODO: 필요한 추가 정보
};
/************
 *MemoryPool
 ************/

class MemoryPool
{
public:
	MemoryPool(int32 allocSize);
	~MemoryPool();

	void Push(MemoryHeader* ptr);
	MemoryHeader* Pop();


private:
	int32 _allocSize = 0; // 메모리 사이즈
	atomic<int32> _allocCount = 0; // 메모리풀에서 뱃어준 갯수

	USE_LOCK;
	queue<MemoryHeader*> _queue;

};

