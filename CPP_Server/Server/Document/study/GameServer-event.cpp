#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <thread>
#include <mutex>
#include <Windows.h>

mutex m;
queue<int32> q;
HANDLE handle;

void Producer()
{
	while(true)
	{
		{
			unique_lock<mutex> lock(m);
			q.push(100);
		}

		::SetEvent(handle); // Signal 상태로 변경

		this_thread::sleep_for(1000ms);		
	}
}

// Producer에 의존하므로
// 계속 체크하는것은 좋은 방법이 아니다.
void Comsumer()
{
	while(true)
	{
		// handle이 signal상태가 되면 밑으로 이동
		// 대기를 하다 처리 (무의미한 while 처리를 하지 않는다.)
		::WaitForSingleObject(handle, INFINITE);

		//::ResetEvent(handle); // Manaual Reset : FALSE면 해줘야 한다.

		// Non-Signal가 되었음
		unique_lock<mutex> lock(m);
		if(q.empty() == false)
		{
			int32 data = q.front();
			q.pop();
			cout << data << endl;
		}
	}
}

int main()
{
	// 커널 오브젝트
	// Usage Count
	// Signal(파란불) / Non-Signal(빨간불) << bool
	// Auto / Manul << bool
	handle = ::CreateEvent(NULL, //보안속성,  
		FALSE, // Manaual Reset
		FALSE, // bInitalState
		NULL);

	thread t1(Producer);
	thread t2(Comsumer);

	t1.join();
	t2.join();

	::CloseHandle(handle);		
}