#include "pch.h"
#include "UserManager.h"
#include "AccountManager.h"

void UserManager::ProcessSave()
{
	// ����� �ذ�: ������accountLock�� ���
	// userLock�� �� ��� ���� ��Ģ�� ��´�.
	Account* account = AccountManager::Instance()->GetAccount(100);

	// accountLock
	lock_guard<mutex> guard(_mutex);

	
}
