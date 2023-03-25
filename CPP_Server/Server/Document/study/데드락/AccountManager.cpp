#include "pch.h"
#include "AccountManager.h"
#include "UserManager.h"

void AccountManager::ProcessLogin()
{
	// accountLock
	lock_guard<mutex> guard(_mutex);

	// accountLock을 잡고
	// userLock을 또 잡은 상황
	User* user = UserManager::Instance()->GetUser(100);

	// TODO
}
