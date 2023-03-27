#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include "CoreMacro.h"
#include "ThreadManager.h"
#include "Memory.h"
#include "Allocator.h"

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
	// [                    [   ]]
	// STL vector에서도 allocator 들을 보내줄수 있다.
	Vector<Knight> v(100); //  우리가 만든

	Map<int32, Knight> m; //  우리가 만든
	m[100] = Knight();
}

