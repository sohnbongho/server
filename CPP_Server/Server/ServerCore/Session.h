#pragma once
#include "IocpCore.h"
#include "IocpEvent.h"
#include "NetAddress.h"

class Service;
/*--------------------
 * Listener
 --------------------*/
class Session : public IocpObject
{
	friend class Listener;
	friend class IocpCore;
	friend class Service;
public:
	Session();
	virtual ~Session();
public:
	/*외부에서 사용*/
	void Send(BYTE* buffer, int32 len);
	void Disconnect(const WCHAR* cause);

	shared_ptr<Service> GetService() { return _service.lock(); }
	void SetService(shared_ptr<Service> service) { _service = service; }

public:
	/*정보 관련*/
	void		SetNetAddress(NetAddress address){	_netAddress = address;}
	NetAddress  GetNetAddress()	{ return _netAddress; }
	SOCKET		GetSocket() { return _socket; }
	bool		IsConnected() { return _connected; }
	SessionRef	GetSessionRef() { return static_pointer_cast<Session>(shared_from_this()); }

private:
	/*인터페이스 구현*/
	virtual HANDLE GetHandle() override;
	virtual void Dispath(class IocpEvent* iocpEvent, int32 numOfBytes = 0) override;

private:
	/*전송 관련*/
	void	RegisterConnect();
	void	RegisterRecv();
	void	RegisterSend(SendEvent* sendEvent);

	void	ProcessConnect();
	void	ProcessRecv(int32 numOfBytes);
	void	ProcessSend(SendEvent* sendEvent, int32 numOfBytes);

	void	HandleError(int32 errorCode);

protected:
	/*컨텐츠 코드에서 오버로딩*/
	virtual void OnConnected() {}
	virtual int32 OnRecv(BYTE* buffer, int32 len)
	{
		return len;
	}
	virtual void OnSend(int32 len)
	{
		
	}
	virtual void OnDisconnected()
	{
		
	}

public:
	// Temp
	BYTE _recvBuffer[1000];

	// [      ] [    ] 겹치지 않게끔 처리하는 것이 핵심 (순환버퍼)
	// Circular Buffer : 복사 비용이 존재
	/*char _sendBuffer[1000];
	int32 _sendLen = 0;*/

private:
	weak_ptr<Service>	_service;
	SOCKET			_socket = INVALID_SOCKET;
	NetAddress		_netAddress = {};
	Atomic<bool>	_connected = false;

private:
	USE_LOCK;

	/*수신 관련*/

	/*송신 관련*/
private:
	/*iocpEvent 재사용*/
	RecvEvent		_recvEvent;
};

