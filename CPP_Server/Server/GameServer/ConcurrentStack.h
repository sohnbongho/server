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

	// 100%Ȯ���� �ƴϹǷ� Try��� ������
	bool TryPop(T& value)
	{
		lock_guard<mutex> lock(_mutex);
		if (_stack.empty())
			return false;

		//empty -> top -> pop				
		value = std::move(_stack.top()); // �����͸� �������� 
		_stack.pop(); // pop����
		return true;		
	}

	// �����Ͱ� ���� ������ ��ٸ��� ������
	void WaitPop(T& value)
	{
		unique_lock<mutex> lock(_mutex);

		// signal�� �ö����� ���
		_condVar.wait(lock, [this] {return _stack.empty() == false; });

		value = std::move(_stack.top()); // �����͸� �������� 
		_stack.pop(); // pop����
	}

	// Emptyüũ���� ���� Pop���� ���� Ÿ�̹��� �߸� �����Ƿ� ���ǹ�
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


