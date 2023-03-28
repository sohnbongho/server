#pragma once
#include "CoreMacro.h"
#include "Allocator.h"

class MemoryPool;

/**************
 * Memory
 * 메모리 풀을 총괄
 **************/
class Memory
{
	enum
	{
		// -1024까지 32단위, -2048까지 128단위로 -4096까지 256단위
		POOL_COUNT = (1024/32) + (1024/128) + (2048/256),
		MAX_ALLOC_SIZE = 4096,
	};

public:
	Memory();
	~Memory();

	void* Allocate(int32 size);
	void Release(void* ptr);

private:
	vector<MemoryPool*> _pools;

	// 메모리 크기 <-> 메모리 풀
	// 0(1) 빠르게 찾기 위한 테이블	
	MemoryPool* _poolTable[MAX_ALLOC_SIZE + 1];

};

template<typename Type, typename ... Args>
Type* xnew(Args&&... args)
{
	Type* memory = static_cast<Type*>(PoolAllocator::Alloc(sizeof(Type)));

	// placement new 문법 (생성자 호출)
	new(memory)Type(std::forward<Args>(args)...);
	return memory;
}

template<typename Type>
void xdelete(Type* obj)
{
	obj->~Type();
	PoolAllocator::Release(obj);
}

template<typename Type>
shared_ptr<Type> MakeShared()
{
	return shared_ptr<Type>{xnew<Type>(), xdelete<Type>};
}