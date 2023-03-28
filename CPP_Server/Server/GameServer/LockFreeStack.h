#pragma once

// ----------------
//		1�� �õ� (�̱۾�����)
// ----------------
/*
template<typename T>
struct Node
{
	T data;
	Node* node;
};

struct SListEntry
{
	SListEntry* next;
};

struct SListHeader
{
	SListEntry* next = nullptr;	
};

// [data][][][]
// Header[next]

void InitializeHead(SListHeader* header);
void PushEntrySList(SListHeader* header, SListEntry* entry);
SListEntry* PopEntrySList(SListHeader* header);
*/

// ----------------
//		2�� �õ� (��Ƽ������)
// ----------------
//struct SListEntry
//{
//	SListEntry* next;
//};
//
//struct SListHeader
//{
//	SListEntry* next = nullptr;
//};
//void InitializeHead(SListHeader* header);
//void PushEntrySList(SListHeader* header, SListEntry* entry);
//SListEntry* PopEntrySList(SListHeader* header);

// ----------------
//	3�� �õ� ABA Problem �ذ�
// ----------------

// �� 16����Ʈ 

// ������ �޸𸮰� 16����Ʈ��
DECLSPEC_ALIGN(16)
struct SListEntry
{
	SListEntry* next;
};

struct SListHeader
{
	SListHeader()
	{
		alignment = 0;
		region = 0;
	}
	union 
	{
		// 128 Byte 
		struct 
		{
			uint64 alignment;
			uint64 region;
		}DUMMYSTRUCTNAME;
		struct
		{
			uint64 depth : 16;
			uint64 sequence : 48;
			uint64 reserved : 4;
			uint64 next : 60;
		}HeaderX64;
	};	
};

void InitializeHead(SListHeader* header);
void PushEntrySList(SListHeader* header, SListEntry* entry);
SListEntry* PopEntrySList(SListHeader* header);

