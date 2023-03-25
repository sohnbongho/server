#include "pch.h"
#include "PlayerManager.h"
#include "AccountManager.h"

PlayerManager GPlayerManager;

void PlayerManager::PlayerThenAcoount()
{
	WRITE_LOCK;	
	// 데드락 타이밍 이슈 체크하기 위해
	//this_thread::sleep_for(1s);
	GAccountManager.Lock();
}

void PlayerManager::Lock()
{
	WRITE_LOCK;
}

