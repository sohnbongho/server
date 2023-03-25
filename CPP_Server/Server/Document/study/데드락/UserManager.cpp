#include "pch.h"
#include "UserManager.h"
#include "AccountManager.h"

void UserManager::ProcessSave()
{
	// 데드락 해결: 무조건accountLock을 잡고
	// userLock을 또 잡게 내부 규칙을 잡는다.
	Account* account = AccountManager::Instance()->GetAccount(100);

	// accountLock
	lock_guard<mutex> guard(_mutex);

	
}
