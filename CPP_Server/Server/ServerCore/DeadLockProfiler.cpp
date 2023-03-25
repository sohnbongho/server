#include "pch.h"
#include "DeadLockProfiler.h"

void DeadLockProfiler::PushLock(const char* name)
{
	LockGuard guard(_lock);

	// ���̵� ã�ų� �߱��Ѵ�.
	int32 lockId = 0;

	auto findIt = _nameToId.find(name);
	if(findIt == _nameToId.end())
	{
		lockId = static_cast<int32>(_nameToId.size());
		_nameToId[name] = lockId;
		_idToName[lockId] = name;
	}
	else
	{
		lockId = findIt->second;
	}
	if(_lockStack.empty() == false)
	{
		// ������ �߰ߵ��� ���� ���̽���� ����� ���� �ٽ� Ȯ���Ѵ�.
		const int32 prevId = _lockStack.top();
		if(lockId != prevId)
		{
			set<int32>& history = _lockHistory[prevId];
			if(history.find(lockId) == history.end())
			{
				history.insert(lockId);
				CheckCycle();
			}
		}
	}
	_lockStack.push(lockId);
}

void DeadLockProfiler::PopLock(const char* name)
{
	LockGuard guard(_lock);
	if (_lockStack.empty())
		CRASH("MULTILE_UNLOCK");

	int32 lockId = _nameToId[name];
	if(_lockStack.top() != lockId)
	{
		CRASH("INVALID_UNLOCK");
	}

	_lockStack.pop();
}

void DeadLockProfiler::CheckCycle()
{
	const int32 lockCount = static_cast<int32>(_nameToId.size());
	_discoverOrder = vector<int32>(lockCount, - 1);
	_discoveredCount = 0;
	_finished = vector<bool>(lockCount, false);
	_parent = vector<int32>(lockCount, -1);

	for(int32 lockId =0; lockId < lockCount; lockId++)
	{
		Dfs(lockId);
	}

	// ������ �������� �����Ѵ�.
	_discoverOrder.clear();
	_finished.clear();
	_parent.clear();	

}


void DeadLockProfiler::Dfs(int32 here)
{
	if (_discoverOrder[here] != -1)
		return;

	_discoverOrder[here] = _discoveredCount++;

	// ��� ������ ������ ��ȸ�Ѵ�.
	auto findIt = _lockHistory.find(here);
	if(findIt == _lockHistory.end())
	{
		_finished[here] = true;
		return;
	}
	set<int32>& nextSet = findIt->second;
	for(int32 there : nextSet)
	{
		// ���� �湮�� ���� ���ٸ� �湮�Ѵ�.
		if(_discoverOrder[there] == -1)
		{
			_parent[there] = here;
			Dfs(there);
			continue;
		}
		// Here�� there���� ���� �߰ߵǾ��ٸ�, there�� here�� �ļ��̴�(������ ����)
		if(_discoverOrder[here] < _discoverOrder[there])
		{
			continue;
		}
		// �������� �ƴϰ�, DFS(there)�� ���� ������� �ʾҴٸ�, there�� here�� �����̴�(������ ����)
		if(_finished[there] == false)
		{
			printf("%s -> %s", _idToName[here], _idToName[there]);

			int32 now = here;
			while(true)
			{
				printf("%s -> %s", _idToName[_parent[now]], _idToName[now]);
				now = _parent[now];
				if (now == there)
				{
					break;
				}
			}

			CRASH("DEADLOCK_DETECTED");
		}
	}
	_finished[here] = true;
	
}
