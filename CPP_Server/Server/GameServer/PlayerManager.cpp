#include "pch.h"
#include "PlayerManager.h"
#include "AccountManager.h"

PlayerManager GPlayerManager;

void PlayerManager::PlayerThenAcoount()
{
	WRITE_LOCK;	
	// ����� Ÿ�̹� �̽� üũ�ϱ� ����
	//this_thread::sleep_for(1s);
	GAccountManager.Lock();
}

void PlayerManager::Lock()
{
	WRITE_LOCK;
}

