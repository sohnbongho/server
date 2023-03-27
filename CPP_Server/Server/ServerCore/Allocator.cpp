#include "pch.h"
#include "Allocator.h"

void* BaseAllocator::Alloc(int32 size)
{
	return ::malloc(size);
}

void BaseAllocator::Release(void* ptr)
{
	::free(ptr);
}

/****************
 * StompAllocator
 ***************/
void* StompAllocator::Alloc(int32 size)
{
	const int64 pageCount = (size + PAGE_SIZE - 1) / PAGE_SIZE;
	const int64 dateOffset = (pageCount * PAGE_SIZE) - size;

	void* baseAddress = ::VirtualAlloc(NULL, pageCount * PAGE_SIZE, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);

	// 크게 잡고 메모리를 뒷부분 부분 주게
	// [                    [   ]]
	return static_cast<void*>(static_cast<int8*>(baseAddress) + dateOffset);
}

void StompAllocator::Release(void* ptr)
{
	// [                    [   ]]
	const int64 address = reinterpret_cast<int64>(ptr);
	const int64 baseAddress = address - (address % PAGE_SIZE);
	::VirtualFree(reinterpret_cast<void*>(baseAddress), 0, MEM_RELEASE);

}

/****************
 * StlAllocator
 ***************/
