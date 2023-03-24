#pragma once

#include <mutex>

template<typename  T>
class LockStack
{
public:
	LockStack()
	{
		
	}
	LockStack(const LockStack&) = delete;
	LockStack& operator=(const LockStack&) = delete;

	void Push(T value)
	{
		lock_guard<mutex> lock(_mutex);
		_stack.push(std::move(value));
		_condVar.notify_one();
	}

	// 100%확율이 아니므로 Try라고 붙이자
	bool TryPop(T& value)
	{
		lock_guard<mutex> lock(_mutex);
		if (_stack.empty())
			return false;

		//empty -> top -> pop				
		value = std::move(_stack.top()); // 데이터를 꺼내오고 
		_stack.pop(); // pop하자
		return true;		
	}

	// 데이터가 있을 때까지 기다리다 가져감
	void WaitPop(T& value)
	{
		unique_lock<mutex> lock(_mutex);

		// signal이 올때까지 대기
		_condVar.wait(lock, [this] {return _stack.empty() == false; });

		value = std::move(_stack.top()); // 데이터를 꺼내오고 
		_stack.pop(); // pop하자
	}

	// Empty체크하자 마자 Pop으로 빼는 타이밍이 잘못 맞으므로 무의미
	/*bool Empty()
	{
		lock_guard<mutex> lock(_mutex);
		return _stack.empty();
	}*/

private:
	stack<T> _stack;
	mutex _mutex;
	condition_variable _condVar;	
};


