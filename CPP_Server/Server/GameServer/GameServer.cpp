#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include "CoreMacro.h"
#include "ThreadManager.h"
#include "Memory.h"
#include "Allocator.h"

class Knight
{
public:
	Knight()
	{
		cout << "Knight " << endl;
		
	}
	~Knight()
	{
		cout << "~Knight " << endl;
	}

public:
	int32 _hp = rand() % 1000;
};

class Monster
{
public:
	int64 _id = 0;
};


SLIST_HEADER* GHeader;


int main()
{
	Knight* knights[100];
	for(int32 i = 0; i < 100; i++)
	{
		knights[i] = ObjectPool<Knight>::Pop();
	}
	for (int32 i = 0; i < 100; i++)
	{
		ObjectPool<Knight>::Push(knights[i]);
		knights[i] = nullptr;
	}

	// 생성자, 딜리터
	{
		shared_ptr<Knight> sptr = ObjectPool<Knight>::MakeShared();  // 오브젝트 풀버전 
	}
	shared_ptr<Knight> sptr2 = MakeShared<Knight>(); // 메모리풀 버전

	

	for (int32 i = 0; i < 2; i++)
	{
		GThreadManager->Launch([]()
			{
				while (true)
				{
					Knight* knight = xnew<Knight>();
					cout << knight->_hp << endl;
					this_thread::sleep_for(10ms);
					xdelete(knight);					
				}
			});
	}

	GThreadManager->Join();
	
}

