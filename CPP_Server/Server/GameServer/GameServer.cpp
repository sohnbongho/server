#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <thread>
#include <mutex>
#include <future>
#include <Windows.h>
#include "CoreMacro.h"
#include "ThreadManager.h"
#include "RefCounting.h"

class Wraight;
class Missile;


using WriaghRef = TSharedPtr<Wraight>;
using MissileRef = TSharedPtr<Missile>;

class Wraight : public RefCountable
{
public:
	virtual ~Wraight()
	{
		int kk = 0;
	}
	int _hp = 150;
	int _posX = 0;
	int _posY = 0;
};

class Missile : public RefCountable
{
public:
	void SetTarget(WriaghRef target)
	{
		_target = target;
		// 멀티 쓰레드에서 중간에 개입
		// target 값이 delete될수도 있다.
		//target->AddRef();
	}
	void Test(WriaghRef target)
	{
		
	}
	bool Update()
	{
		if(_target == nullptr)
		{
			return true;
		}

		int posX = _target->_posX;
		int posY = _target->_posY;

		// TODO: 쫓아간다.
		if(_target->_hp == 0)
		{			
			_target = nullptr;
			return true;
		}
		return false;
	}

	WriaghRef _target;
	
};

int main()
{
	WriaghRef wraight(new Wraight());
	wraight->ReleaseRef(); // ref 2값이 되는데 1로 변경
	MissileRef missile(new Missile());
	missile->ReleaseRef(); // 

	missile->SetTarget(wraight); // 복사 연산자 실행 ref2로 감

	// 레이스가 피격 당함.
	wraight->_hp = 0;	
	auto* pWraight = &wraight;
	wraight = nullptr; // ==> wraight = WriaghRef(nullptr);

	while (true)
	{
		if(missile)
		{
			if(missile->Update())
			{				
				missile = nullptr;				
			}
		}
	};
	missile = nullptr;
	
}
