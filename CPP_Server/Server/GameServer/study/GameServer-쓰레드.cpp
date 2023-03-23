#include "pch.h"
#include <iostream>
#include "CorePch.h"

#include <thread>

// Entry Point
void HelloThread()
{
	cout << "Hello Thread" << endl;
}

void HelloThread_2(int32 num)
{
	cout << num << endl;
}

int main()
{	
	{
		std::thread t1(HelloThread);

		cout << "Hello Main" << endl;

		int32 count = t1.hardware_concurrency(); // cpu 코어 개수를 가져온다.
		auto id = t1.get_id(); // 쓰레드마다 id

		// t쓰레드랑 메인 thread랑 연결을 끊는다.
		// 빽에서 동작하게 한다.
		// std::thread객체에서 실제 쓰레드를 분리
		// 실제 쓸일이 있을까??
		//t1.detach();
		
		// 쓰레드가 끝날때 까지 기다린다.
		t1.join();  
		
	}
	{
		std::thread t2;

		auto id = t2.get_id(); // 쓰레드마다 id

		// 쓰레드 객체만 만들고 아직 시작안했을 때,
		// 시작이 가능한지? 
		auto t2Joinable = t2.joinable();
		t2 = std::thread(HelloThread);


		id = t2.get_id(); // 쓰레드마다 id
		t2Joinable = t2.joinable();

		if(t2.joinable())
		{
			t2.join();
		}
	}

	{
		// thread 함수에 파라미터를 보낸다.
		std::thread t(HelloThread_2, 10);		

		if (t.joinable())
		{
			t.join();
		}
	}
	{
		vector<std::thread> v;
		for(int32 i =0 ; i < 10 ; ++i)
		{
			// thead는 누가 먼저 실행할지 알수 가 없다.
			v.push_back(std::thread(HelloThread_2, i));
		}

		for (int32 i = 0; i < 10; ++i)
		{
			if(v[i].joinable())
			{
				v[i].join();
			}
		}
		
	}
}

