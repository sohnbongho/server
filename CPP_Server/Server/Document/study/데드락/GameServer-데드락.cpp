#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <thread>
#include <mutex>
#include "UserManager.h"
#include "AccountManager.h"

void Func1()
{
	for(int32 i =0; i< 10000; ++i)
	{
		UserManager::Instance()->ProcessSave();
	}
}

void Func2()
{
	for (int32 i = 0; i < 10000; ++i)
	{
		AccountManager::Instance()->ProcessLogin();
	}
}

int main()
{
	// 전형적인 DeadLock상황
	// t1 : User매니저 Lock걸고, Account쪽 Lock
	// t2 : Account매니저 Lock걸고, User쪽 Lock 
	std::thread t1(Func1);
	std::thread t2(Func2);

	t1.join();
	t2.join();

	cout << "Jobs Done" << endl;

	// 참고
	mutex m1;
	mutex m2;
	std::lock(m1, m2); // m1.lock(), m2.lock() 이런 기능도 있다.

	// adopt_lock: 이미 lock된 상태니까, 나중에 소멸될 때 풀어주기만 해
	lock_guard<mutex> g1(m1, std::adopt_lock);
	lock_guard<mutex> g2(m2, std::adopt_lock);
}

