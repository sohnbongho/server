#pragma once
#include "Types.h"

/*
 * 표준 뮤텍스 문제점
 * 1) 재귀적으로 Lock을 잡을수가 없다.
 *		Lock을 잡은 상태에서 또 Lock을 잡을수가 없다.
 * 2) 
 * 3) 정책을 우리 마음대로 골라줄수 있다.
 * 
 */

/********************
 * RW SpinLock
********************/

/********************
 *
 * [WWWWWWWW][WWWWWWWW][RRRRRRRR][RRRRRRRR]
 * W : WriteFlag (Exclusive Lock Owner ThreadId)
 * R : ReadFlag (Shared Lock Count)
********************/

// W -> W (0)
// W -> R (0)
// R -> W (X)

class Lock
{
	enum :uint32
	{
		ACQUIRE_TIMEOUT_TICK = 10000,
		MAX_SPIN_COUNT = 5000,
		WRTIE_THREAD_MASK = 0xFFFF'0000,
		READ_COUNT_MASK = 0x0000'FFFF,
		EMPTY_FLAG = 0x0000'0000
	};
public:
	void WriteLock();
	void WriteUnLock();
	void ReadLock();
	void ReadUnLock();

private:
	Atomic<uint32> _lockFlag = EMPTY_FLAG;
	uint16 _writeCount = 0;
};

/***********************
 * Lock Guard 
 ***********************/
class ReadLockGuard
{
public:
	ReadLockGuard(Lock& lock) : _lock(lock) { _lock.ReadLock(); }
	~ReadLockGuard() { _lock.ReadUnLock(); }

private:
	Lock& _lock;
};

class WriteLockGuard
{
public:
	WriteLockGuard(Lock& lock) : _lock(lock) { _lock.WriteLock(); }
	~WriteLockGuard() { _lock.WriteUnLock(); }

private:
	Lock& _lock;



};

