#include "pch.h"
#include "SendBuffer.h"

/*------------------
 * SendBuffer
 ------------------*/
SendBuffer::SendBuffer(SendBufferChunkRef owner, BYTE* buffer, int32 allocSize)
	: _owner(owner), _buffer(buffer), _allocSize(allocSize)
{
	
}

SendBuffer::~SendBuffer()
{	
}

void SendBuffer::Close(uint32 writeSize)
{
	ASSERT_CRASH(_allocSize >= writeSize);
	_writeSize = writeSize;
	_owner->Close(writeSize);
}

/*------------------
 * SendBufferChunk
 * : 큰 send buffer를 만들고 짤라서 쓰는 방식
 * TLS영역에서 실행되므로 멀티 쓰레드를 고려하지 않아도 된다.
 ------------------*/
SendBufferChunk::SendBufferChunk()
{
}

SendBufferChunk::~SendBufferChunk()
{
}

void SendBufferChunk::Reset()
{
	_open = false;
	_usedSize = 0;
}

SendBufferRef SendBufferChunk::Open(uint32 allocSize)
{
	ASSERT_CRASH(allocSize <= SEND_BUFFER_CHUNK_SIZE);
	ASSERT_CRASH(_open == false);

	if (allocSize > FreeSize())
		return nullptr;

	_open = true;
	return ObjectPool<SendBuffer>::MakeShared(shared_from_this(), Buffer(), allocSize);
}

void SendBufferChunk::Close(uint32 writeSize)
{
	ASSERT_CRASH(_open == true);
	_open = false;
	_usedSize += writeSize;
}

/*------------------
 * SendBufferManager
 ------------------*/
SendBufferRef SendBufferManager::Open(uint32 size)
{
	// [[  ]            ]
	// 우리가 사용할 만큼만 가져가는 함수

	// 쓰레드마다 고유한 TLS영역
	if (LSendBufferChunk == nullptr)
	{
		LSendBufferChunk = Pop(); // WRTE_LOCL;
		LSendBufferChunk->Reset();
	}

	ASSERT_CRASH(LSendBufferChunk->IsOpen() == false);

	// 다 썻으면 버리고 새거로 교체
	if(LSendBufferChunk->FreeSize() < size)
	{
		// 데이터 여유분이 없으면 새로 만듬
		LSendBufferChunk = Pop(); //
		LSendBufferChunk->Reset();
	}

	cout << "Free : " << LSendBufferChunk->FreeSize() << endl;

	return LSendBufferChunk->Open(size);
	
}

SendBufferChunkRef SendBufferManager::Pop()
{
	{
		WRITE_LOCK;
		if(_sendBufferChunks.empty() == false)
		{
			SendBufferChunkRef sendBufferChunk = _sendBufferChunks.back();
			_sendBufferChunks.pop_back();
			return sendBufferChunk;			
		}		
	}
	// 여유분이 없다면 메모리 생성
	return SendBufferChunkRef(xnew<SendBufferChunk>(), PushGlobal);

}

void SendBufferManager::Push(SendBufferChunkRef buffer)
{
	WRITE_LOCK;
	_sendBufferChunks.push_back(buffer);
}

void SendBufferManager::PushGlobal(SendBufferChunk* buffer)
{
	// 삭제될때 호출
	GSendBufferManager->Push(SendBufferChunkRef(buffer, PushGlobal));

}
