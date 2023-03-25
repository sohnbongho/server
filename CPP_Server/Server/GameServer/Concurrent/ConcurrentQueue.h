#pragma once
#include <mutex>


template<typename T>
class LockQueue
{
public:
	LockQueue(){}

	LockQueue(const LockQueue&) = delete;
	LockQueue& operator=(const LockQueue&) = delete;

	void Push(T value)
	{
		lock_guard<mutex> lock(_mutex);
		_queue.push(std::move(value));
		_condVar.notify_one();
	}

	// 100%확율이 아니므로 Try라고 붙이자
	bool TryPop(T& value)
	{
		lock_guard<mutex> lock(_mutex);
		if (_queue.empty())
			return false;

		//empty -> top -> pop				
		value = std::move(_queue.front()); // 데이터를 꺼내오고 
		_queue.pop(); // pop하자
		return true;
	}

	// 데이터가 있을 때까지 기다리다 가져감
	void WaitPop(T& value)
	{
		unique_lock<mutex> lock(_mutex);

		// signal이 올때까지 대기
		_condVar.wait(lock, [this] {return _queue.empty() == false; });

		value = std::move(_queue.front()); // 데이터를 꺼내오고 
		_queue.pop(); // pop하자
	}

private:
	queue<T> _queue;
	mutex _mutex;
	condition_variable _condVar;
};