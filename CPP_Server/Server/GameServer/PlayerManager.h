#pragma once
class PlayerManager
{
	USE_LOCK;
public:
	void PlayerThenAcoount();
	void Lock();
};

extern PlayerManager GPlayerManager;
