#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <thread>
#include <mutex>
#include <future>
#include <Windows.h>
#include "CoreMacro.h"
#include "ThreadManager.h"
#include "memory.h"

#include "RefCounting.h"

 
class Knight
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
	~Knight()
	{
		cout << "~Knight" << endl;
	}
	/*static void* operator new(size_t size)
	{
		cout << "knight new! " << size << endl;
		void* ptr = ::malloc(size);
		return ptr;
	}

	static void operator delete(void* ptr)
	{
		cout << "knight delete! " << endl;
		::free(ptr);
	}*/
private:
	int32 _hp;
};

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


int main()
{
	Knight* knight = xnew< Knight>(100);

	xdelete(knight);
}

