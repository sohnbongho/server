#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <thread>
#include <mutex>

// 멀티 쓰레드 환경에서 문제 발생
/*
1)
	[1][2][3]
	[1][2][3][][][]
	vector size를 바꿀 때, 다른 thread에서도 같은 행위를 하기에 오류 발생
	double free문제가 발생할것으로 예상

2) v.reserve(20000);
	큰 vector크기를 잡아주어도 문제 발생
	동시 다발적으로 같은 메모리에 기입할수 있는 상황도 발생

따라서 확실한 방법은 한번에 한번씩 메모리 접근(-> Lock을 활용)	
	
*/
vector<int32> v;

// Mutual Exclusive(상호 배타적)
mutex m; // Lock을 하는 자물쇠

// RAII 패턴(Resouce Acquisition is initailization)
template<typename T>
class LockGuard
{
public:
	LockGuard(T& m)
	{
		_mutex = &m;
		_mutex->lock();
	}
	~LockGuard()
	{
		_mutex->unlock();		
	}

private:
	T* _mutex;
};

void Push()
{
	std::lock_guard<std::mutex> lockGuard(m); // RAII를 이용한 lock 처리

	for(int32 i = 0; i < 10000; ++i)
	{		
		// 자물쇠 잠그기
		// unlock으로 풀어주기 전까지 대기		
		//m.lock();
		//m.lock();		 // mutex는 제귀적으로 호출하면 오류
		// mutex를 생으로 쓰는거 보다 RAII를 이용하여 lock처리하는 것이 좋은 코드이다.
		//LockGuard<std::mutex> lockGuard(m); // RAII를 이용한 lock 처리
		//std::lock_guard<std::mutex> lockGuard(m); // RAII를 이용한 lock 처리
		//std::unique_lock<std::mutex> uniqueLock(m, std::defer_lock); //  지금 당장 lock하지 않고 잠기는 시점을 뒤로 미룰수 있다.

		//uniqueLock.lock(); // lock_guard보다는 조금 용량이 크다.

		// 싱글 쓰레드로 동작하는 것이기에 lock과 unlock이므로 속도가 느리다.
		v.push_back(i);		

		// 자물쇠 풀기
		//m.unlock();
		//m.unlock();	// mutex는 제귀적으로 호출하면 오류
	}
}

int main()
{
	//v.reserve(20000);

	std::thread t1(Push);
	std::thread t2(Push);

	t1.join();
	t2.join();

	cout << v.size() << endl;
	
}

