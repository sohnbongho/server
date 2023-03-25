#include "pch.h"
#include <iostream>
#include "CorePch.h"

#include <thread>
#include <atomic>

// atomic : All-Or-Nothing

// DB
// atomic하게 DB에서 트랙잭션을 한번에 처리
//(A라는 유저 인벤에서 집행검을 빼고
// B라는 유저 인벤에 집행검을 추가)

//int32 sum = 0;
atomic<int32> sum = 0;

void Add()
{
	for(int32 i = 0; i< 100'0000; ++i)
	{
		// 일반 변수로 하면 아래와 같이 3줄로 실행
		// 00007FF7DD572745  mov         eax,dword ptr [sum (07FF7DD57F440h)]
		// 00007FF7DD57274B  inc         eax
		// 00007FF7DD57274D  mov         dword ptr[sum(07FF7DD57F440h)], eax
		// 어셈코드를 보면 한줄이 아니라 3줄이다.

		//sum++;
		sum.fetch_add(1);  // atomic 를 이용하여 처리
		// atomic 로 하면 1줄로 변경
		// 00007FF703C72897  call        std::_Atomic_integral<int,4>::fetch_add (07FF703C71267h)

		// atomic은 속도가 느려 사용하는데 있어 신중해야 한다.
	}
	
}

void Sub()
{
	for (int32 i = 0; i < 100'0000; ++i)
	{		
		//sum--; // 어셈코드를 보면 한줄이 아니라 3줄이다.
		sum.fetch_add(-1); // atomic 를 이용하여 처리
	}
}


int main()
{
	Add();
	Sub();
	cout << sum << endl;

	std::thread t1(Add);
	std::thread t2(Sub);

	t1.join();
	t2.join();
	cout << sum << endl;
}

