#pragma once

/*
 * �پ��� ũ�⸦ ������ �ִ� Ǯ��
 * �Ʒ��� ���� 2���� ����� ����
 * 1) ���� ũ�� ��ȯ
 *		[32][64][][][][][][][]
 * 2) ������ ũ�� ���� ��ȯ
 *		[32 32 32 32 32 32]
 */

enum
{
	SLIST_ALIGNMENT = 16
};

/************
 *MemeryHeadr
 ************/

DECLSPEC_ALIGN(SLIST_ALIGNMENT)
struct MemoryHeader : public SLIST_ENTRY
{
	// [MemoryHeader][Data]
	MemoryHeader(int32 size) : allocSize(size)
	{
		
	}
	static void* AttachHeader(MemoryHeader* header, int32 size)
	{
		new(header)MemoryHeader(size);// placement New
		return reinterpret_cast<void*>(++header); // ������ ������ġ
	}
	static MemoryHeader* DetachHeader(void* ptr)
	{
		MemoryHeader* header = reinterpret_cast<MemoryHeader*>(ptr) - 1;
		return header;
	}	

	int32 allocSize;
	// TODO: �ʿ��� �߰� ����
};
/************
 *MemoryPool
 ************/

DECLSPEC_ALIGN(SLIST_ALIGNMENT)
class MemoryPool
{
public:
	MemoryPool(int32 allocSize);
	~MemoryPool();

	void Push(MemoryHeader* ptr);
	MemoryHeader* Pop();


private:
	SLIST_HEADER _header;
	int32 _allocSize = 0; // �޸� ������
	atomic<int32> _usedCount = 0; // �޸�Ǯ���� ����� ����
	atomic<int32> _reserveCount = 0; // �޸�Ǯ���� ����� ����

};

