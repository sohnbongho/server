#pragma once

/*
 * �پ��� ũ�⸦ ������ �ִ� Ǯ��
 * �Ʒ��� ���� 2���� ����� ����
 * 1) ���� ũ�� ��ȯ
 *		[32][64][][][][][][][]
 * 2) ������ ũ�� ���� ��ȯ
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

class MemoryPool
{
public:
	MemoryPool(int32 allocSize);
	~MemoryPool();

	void Push(MemoryHeader* ptr);
	MemoryHeader* Pop();


private:
	int32 _allocSize = 0; // �޸� ������
	atomic<int32> _allocCount = 0; // �޸�Ǯ���� ����� ����

	USE_LOCK;
	queue<MemoryHeader*> _queue;

};

