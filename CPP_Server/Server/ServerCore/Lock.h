#pragma once
#include "Types.h"

/*
 * ǥ�� ���ؽ� ������
 * 1) ��������� Lock�� �������� ����.
 *		Lock�� ���� ���¿��� �� Lock�� �������� ����.
 * 2) 
 * 3) ��å�� �츮 ������� ����ټ� �ִ�.
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

