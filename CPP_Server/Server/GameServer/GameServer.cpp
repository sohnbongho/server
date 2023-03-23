#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <thread>
#include <mutex>
#include <Windows.h>


mutex m;
queue<int32> q;

// 참고) CV는 User-Level Object(커널 오브젝트X)
condition_variable cv; 


void Producer()
{
	while(true)
	{
		// 1) Lock을 잡고
		// 2) 공유 변수 값을 수정
		// 3) Lock을 풀고
		// 4) 조건 변수 통해 다른 쓰레드에게 통지
		{
			unique_lock<mutex> lock(m);
			q.push(100);			
		}

		cv.notify_one(); // Wait중인 쓰레드가 있으면, 딱 하나의 쓰레드만 깨운다.
		this_thread::sleep_for(1000ms);
	}
}

// Producer에 의존하므로
// 계속 체크하는것은 좋은 방법이 아니다.
void Comsumer()
{
	while(true)
	{
		// Non-Signal가 되었음
		unique_lock<mutex> lock(m);
		cv.wait(lock, []() {return q.empty() == false; });
		// 1) Lock을 잡고
		// 2) 조건 확인
		// 조건 만족0-> 빠져나와 이어서 코드를 진행. Lock을 잡음
		// 조건 만족X-> Lock을 풀어주고 대기 상태

		// 그런데 notify_one을 했으면 항상 조건식으로 만족하는거 아닐까?
		// Spurious Wakeup(가짜 기상)
		// notify_one을 할때, lock을 잡고 있는 것이 아니기 때문
		{
			int32 data = q.front();
			q.pop();
			cout << data << " size : " << q.size() << endl;
		}
	}
}

int main()
{
	thread t1(Producer);
	thread t2(Comsumer);

	t1.join();
	t2.join();

}