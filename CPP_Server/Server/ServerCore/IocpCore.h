#pragma once

/*------------
 *IocpObject
 ------------*/
class IocpObject
{
public:
	virtual HANDLE GetHandle() abstract;
	virtual void Dispath(class IocpEvent* iocpEvent, int32 numOfBytes = 0) abstract;
};

/*------------
 *IocpCore
 ------------*/
class IocpCore
{
public:
	IocpCore();
	~IocpCore();

	HANDLE	GetHandel() { return _iocpHandle; }

	bool	Register(class IocpObject* iocpObject);
	bool	Dispatch(uint32 timeoutMs = INFINITE); // �ϰ��� �ֳ� ���� ���

private:
	HANDLE _iocpHandle;
};

// TEMP
extern IocpCore GIocpCore;