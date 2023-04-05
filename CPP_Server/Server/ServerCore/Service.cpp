#include "pch.h"
#include "Service.h"
#include "Session.h"
#include "Listener.h"

/*---------------
 * Service
 ---------------*/
Service::Service(ServiceType type, NetAddress address, IocpCoreRef core, SessionFactory factory, int32 maxSessionCount)
	: _type(type), _netAddress(address), _iocpCore(core), _sessionFactory(factory), _maxSessionCount(maxSessionCount)
{

}

Service::~Service()
{
}

void Service::CloseService()
{
	// TODO	
}

SessionRef Service::CreateSession()
{
	SessionRef session = _sessionFactory();
	session->SetService(shared_from_this());

	if (_iocpCore->Register(session) == false)
		return nullptr;

	return session;
}

void Service::AddSession(SessionRef session)
{
	WRITE_LOCK;
	_sessionCount++;
	_sessions.insert(session);
}

void Service::ReleaseSession(SessionRef session)
{
	WRITE_LOCK;
	ASSERT_CRASH(_sessions.erase(session) != 0);
	_sessionCount--;
}

/*---------------
 * Client Service
 ---------------*/
ClientService::ClientService(NetAddress targetAddress, IocpCoreRef core, SessionFactory factory, int32 maxSessionCoumt)
	:Service(ServiceType::Client, targetAddress, core, factory, maxSessionCoumt)
{
}

bool ClientService::Start()
{
	// TODO
	return true;
}

/*---------------
* Server Service
---------------*/
ServerService::ServerService(NetAddress address, IocpCoreRef core, SessionFactory factory, int32 maxSessionCoumt)
	:Service(ServiceType::Server, address, core, factory, maxSessionCoumt)
{

}

bool ServerService::Start()
{
	if (CanStart() == false)
		return false;

	_listener = MakeShared<Listener>();
	if (_listener == nullptr)
		return false;
		
	ServerServiceRef service = static_pointer_cast<ServerService>(shared_from_this());

	if (_listener->StartAccept(service) == false)
		return false;

	// TODO
	return true;
}


void ServerService::CloseService()
{
	// TODO:
	Service::CloseService();
}


