#pragma once

/*------------
 *IocpObject
 ------------*/
class IocpObject : public enable_shared_from_this<IocpObject>
{
public:
	virtual HANDLE GetHandle() abstract;
	virtual void DisPatch(class IocpEvent* iocpEvent, int32 numOfBytes = 0) abstract;
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

	bool	Register(IocpObjectRef iocpObject);
	bool	DisPatch(uint32 timeoutMs = INFINITE); // �ϰ��� �ֳ� ���� ���

private:
	HANDLE _iocpHandle;
};
