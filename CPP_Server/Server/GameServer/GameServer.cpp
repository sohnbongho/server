#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include "CoreMacro.h"
#include "ThreadManager.h"
#include "Memory.h"

// new operator overloading (Global)
//void* operator new(size_t size)
//{
//	cout << "new! " << size << endl;
//	void* ptr = ::malloc(size);
//	return ptr;
//}
//
//void operator delete(void* ptr)
//{
//	cout << "delete! " << endl;
//	::free(ptr);
//}
//
//void* operator new[](size_t size)
//{
//	cout << "new! " << size << endl;
//	void* ptr = ::malloc(size);
//	return ptr;
//}
//
//void operator delete[](void* ptr)
//{
//	cout << "delete! " << endl;
//	::free(ptr);
//}

class Player
{
public:
	Player(){}
	virtual ~Player(){}
};

class Knight : public Player
{
public:
	Knight()
	{
		cout << "Knight" << endl;
	}
	Knight(int hp) : _hp(hp)
	{
		cout << "Knight : " << _hp << endl;
	}
	virtual ~Knight()
	{
		cout << "~Knight" << endl;
	}	
public:
	int32 _hp;
	int32 _mp;
};


int main()
{
	
	// Knight 메모리 > Player메모리가 더 큰 상황에서
	// 아래와 같이 메모리를 잡으면 문제이다!!!
	// 아레 문제를 해결하기 위해 메모리를 잡을때,
	// 뒤부분부터 메모리를 잡아준다면??
	// [                    [   ]]

	Knight* knight = (Knight *)xnew< Player>();
	knight->_mp = 100;
	xdelete(knight);
}

