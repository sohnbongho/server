#include "pch.h"
#include "LockFreeStack.h"

/*
void InitializeHead(SListHeader* header)
{
	header->next = nullptr;
}


void PushEntrySList(SListHeader* header, 
	SListEntry* entry)
{
	entry->next = header->next;
	header->next = entry;
}

SListEntry* PopEntrySList(SListHeader* header)
{
	SListEntry* first = header->next;
	if (first != nullptr)
	{
		header->next = first->next;
	}
	return first;
}
*/
// ----------------
//		2차 시도
// ----------------
/*
void InitializeHead(SListHeader* header)
{
	header->next = nullptr;
}


void PushEntrySList(SListHeader* header,
	SListEntry* entry)
{
	entry->next = header->next;

	while (::_InterlockedCompareExchange64((int64*)&header->next, 
		(int64)entry, (int64)entry->next) == 0)
	{
		
	}	

}

// [][][]
// Header [ next ]
SListEntry* PopEntrySList(SListHeader* header)
{
	SListEntry* expected = header->next;

	// ABA Problem
	// 값을 꺼냈는데, 추후 데이터가 다시 이전과 동일한 값이면??

	// 해결대안: 값을 넣얼 떄 마다, 티겟을 발급
	// 값 하나만 비교하는것이 아니고 count(티켓)도??		

	// 만약에 header 5000이라면 Header에다 6000을 넣어줘
	// [5000] -> [6000] -> [7000]
	// [Header]

	// 데이터가 사라지고 다시 넣었는데 5000이 될지도?
	// [5000] -> [7000]
	// -> [7000]
	// [HEADER] 

	while(expected 
		&& ::_InterlockedCompareExchange64((int64*)&header->next,
		(int64)expected->next, 
		(int64)expected) == 0)
	{

	}

	return expected;
}
*/

// ----------------
//	3차 시도 ABA Problem 해결
// ----------------
void InitializeHead(SListHeader* header)
{
	header->alignment = 0;
	header->region = 0;
}
void PushEntrySList(SListHeader* header, SListEntry* entry)
{
	SListHeader expected = {};
	SListHeader desired = {};

	// 16바이트 정렬
	desired.HeaderX64.next = (((uint64)entry) >> 4);

	while(true)
	{
		expected = *header;

		// 이 사이에 변경될 수 있다.
		entry->next = (SListEntry*)((uint64)expected.HeaderX64.next << 4);
		desired.HeaderX64.depth = expected.HeaderX64.depth + 1;
		desired.HeaderX64.sequence= expected.HeaderX64.sequence + 1;

		if(::_InterlockedCompareExchange128((int64*)header, desired.region, desired.alignment, (int64*)&expected) == 1)
		{
			break;
		}


	}
}

// []
// [5000][6000][7000]
// Header [ next ]
SListEntry* PopEntrySList(SListHeader* header)
{
	SListHeader expected = {};
	SListHeader desired = {};
	SListEntry* entry = nullptr;

	while (true)
	{
		expected = *header;

		entry = (SListEntry*)((uint64)expected.HeaderX64.next << 4);
		if (entry == nullptr)
			break;

		// Use-After-Free
		desired.HeaderX64.next = ((uint64)entry->next) >> 4;
		desired.HeaderX64.depth = expected.HeaderX64.depth - 1;
		desired.HeaderX64.sequence = expected.HeaderX64.sequence + 1;

		if (::_InterlockedCompareExchange128((int64*)header, desired.region, desired.alignment, (int64*)&expected) == 1)
		{
			break;
		}		
	}
	return entry;
}


