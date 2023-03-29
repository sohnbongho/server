#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include "CoreMacro.h"
#include "ThreadManager.h"
#include "Memory.h"
#include "Allocator.h"

using TL = TypeList<class Player, class Mage, class Knight, class Archer>;

class Player
{	
	
public:
	Player()
	{
		INIT_TL(Player)
	}
	virtual ~Player()
	{		
	}
	DECLARE_TL
	
};

class Knight : public Player
{	
public:
	Knight() {
		INIT_TL(Knight);
	}
	
};

class Mage : public Player
{
public:
	Mage() {
		INIT_TL(Mage);
	}

};

class Archer: public Player
{
public:
	Archer() {
		INIT_TL(Archer);
	}

};

class Dog 
{
public:
	Dog() {		
	}

};


int main()
{
	//TypeList<Mage, Knight>::Head whoAmI;
	//TypeList<Mage, Knight>::Tail whoAmI2;

	//TypeList<Mage, TypeList<Knight, Archer>>::Head whoAmI3; // Mage
	//TypeList<Mage, TypeList<Knight, Archer>>::Tail::Head whoAmI4; // Knight
	//TypeList<Mage, TypeList<Knight, Archer>>::Tail::Tail whoAmI5; // Archer

	//int32 len1 = Length<TypeList<Mage, Knight>>::value; // 2
	//// 제귀적 동작
	//// 1 + 1 + 1 + 0
	//int32 len = Length<TypeList<Mage, Knight, Archer>>::value; // 3

	//
	//TypeAt<TL, 0>::Result whoAmI6; // Mage
	//TypeAt<TL, 1>::Result whoAmI7; // Knight
	//TypeAt<TL, 2>::Result whoAmI8; // Archer

	//int32 index1 = IndexOf<TL, Mage>::value; // 0
	//int32 index2 = IndexOf<TL, Knight>::value; // 1
	//int32 index3 = IndexOf<TL, Dog>::value; // -1

	//bool canConver1 = Conversion<Player, Knight>::exists; // Player -> Knight 로 변환이 가능한가? (ERROR)
	//bool canConver2 = Conversion<Knight, Player>::exists; // Knight -> Player 로 변환이 가능한가? (OK)
	//bool canConver3 = Conversion<Player, Dog>::exists; // Player -> Dog 로 변환이 가능한가? (ERROR

	{
		Player* player = new Player();

		// 변환 못하는게 맞다!!
		bool canCast = CanCast<Knight*>(player);
		Knight* knight = TypeCast<Knight*>(player);

		delete player;
	}
	{
		Player* player = new Knight();

		// 변환 성공!!!
		bool canCast = CanCast<Knight*>(player);
		Knight* knight = TypeCast<Knight*>(player);

		delete player;
	}
	{
		shared_ptr<Knight> knight = MakeShared<Knight>();

		shared_ptr<Player> player = TypeCast<Player>(knight);
		bool canCast= CanCast<Player>(knight);
		
	}
	{
		shared_ptr<Player> knight = MakeShared<Knight>();

		shared_ptr<Archer> archer = TypeCast<Archer>(knight);
		bool canCast = CanCast<Mage>(knight);
	}
	

	int a = 0;
	

	/*for (int32 i = 0; i < 2; i++)
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
	}*/

	GThreadManager->Join();
	
}

