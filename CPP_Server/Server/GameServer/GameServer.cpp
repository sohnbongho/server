#include "pch.h"
#include <iostream>
#include "CorePch.h"
#include <thread>
#include <mutex>
#include <future>
#include <Windows.h>
#include "CoreMacro.h"
#include "ThreadManager.h"
#include "Lock.h"

#include "PlayerManager.h"
#include "AccountManager.h"



int main()
{
	GThreadManager->Launch([=]
	{
		while(true)
		{
			cout << "PlayerThenAccount" << endl;
			GPlayerManager.PlayerThenAcoount();
			this_thread::sleep_for(100ms);
		}
	});

	GThreadManager->Launch([=]
	{
		while (true)
		{
			cout << "AccountThenPlayer" << endl;
			GAccountManager.AccountThenPlayer();
			this_thread::sleep_for(100ms);
		}
	});

	GThreadManager->Join();
}

