#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <thread>
#include <mutex>

class SpinLock
{
public:
	// Lock획득하는 함수
	void lock()
	{
		
		//while(_locked)
		//{
		//	
		//}
		//_locked = true; // 두가지 쓰레드가 동시에 변수 변경!!, 위의 while문과 set값이 동시에 이루어져야 한다.!

		// CAS (Campare-And-Swap)
		bool expected = false;
		bool desired = true;

		// CAS 의사 코드
		//if(_locked == expected)
		//{
		//	// Lock을 획득했다.
		//	expected = _locked;
		//	_locked = desired; 		
		//}
		//else
		//{
		//	// 다른 유저가 Lock획득
		//}

		// 두가지 쓰레드가 동시에 변수 변경!!, 위의 while문과 set값이 동시에 이루어 졌다.
		while (_locked.compare_exchange_strong(expected, desired) == false)
		{
			expected = false;
		}		
	}
	void unlock()
	{
		_locked.store(false);
		
	}
private:
	// volatile : 컴파일에게 최적화만 하지 말아달라. c#과 다르게 그냥 최적화만 말아죠
	atomic<bool>_locked = false; 
};

mutex m;
int32 sum = 0;
SpinLock spinLock;

void Add()
{
	for (int32 i = 0; i < 100000; ++i)
	{
		lock_guard<SpinLock> guard(spinLock);
		sum++;
	}

}
void Sub()
{
	for (int32 i = 0; i < 100000; ++i)
	{
		lock_guard<SpinLock> guard(spinLock);
		sum--;
	}

}

int main()
{
	thread t1(Add);
	thread t2(Sub);

	t1.join();
	t2.join();

	cout << sum << endl;	
}

