#pragma once

class Session;

enum class EventType : uint8
{
	Connect,
	Accept,
	//PrevRecv, 0 byte recv
	Recv,
	Send,
};

/*-------------------
 * IocpEvent
 -------------------*/
class IocpEvent : public OVERLAPPED
{
public:
	IocpEvent(EventType type);
	// 여기서 파괴자를 쓰면 안된다. 왜냐하면 가상함수테이블에 파괴자가 들어가면 형변화시, 오류가 발생한다.

	void Init();
	EventType GetType() { return _type; }

protected:
	EventType _type;
};

/*-------------------
 * ConnectEvent
 -------------------*/
class ConnectEvent : public IocpEvent
{
public :
	ConnectEvent() : IocpEvent(EventType::Connect){}

};

/*-------------------
* AcceptEvent
-------------------*/
class AcceptEvent : public IocpEvent
{
public:
	AcceptEvent() : IocpEvent(EventType::Accept) {}

	void SetSession(Session* session) { _session = session; }
	Session* GetSession() {
		return _session;
	}

private:
	Session* _session = nullptr;

};

/*-------------------
* RecvEvent
-------------------*/
class RecvEvent : public IocpEvent
{
public:
	RecvEvent() : IocpEvent(EventType::Recv) {}

};
/*-------------------
* SendEvent
-------------------*/
class SendEvent : public IocpEvent
{
public:
	SendEvent() : IocpEvent(EventType::Send) {}

};