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

template<typename  T>
class LockFreeStack
{
	struct Node
	{
		Node(const T& value) :data(value), next(nullptr)
		{			
		}
		T data;
		Node* next;
		
	};
public:

	// 1) 새 노드를 만들고
	// 2) 새노드의 next = head;
	// 3) head = 새노드

	// [value][][][][][]
	// [head]
	void Push(const T& value)
	{
		Node* node = new Node(value);
		node->next = _head;

		// 이 사이에 새치기 당하면?
		//_head = node;

		/*if(_head == node->next)
		{
			_head = node;
			return true;
		}
		else
		{
			node->next = _head;
			return false;
		}*/
		while (_head.compare_exchange_weak(node->next, node) == false)
		{
			//node->next = _head;						
		}
		// Lock-Free라 해도 경합이 없을 수는 없다.
	}

	// 1) head 읽기
	// 2) head->next 읽기
	// 3) head = head->next
	// 4) data 추출해서 반환
	// 5) 추출한 노드를 삭제

	// [ ][ ][ ][ ][ ]
	// [head]
	bool TryPop(T& value)
	{
		++_popcount;

		Node* oldHead = _head;
		
		/*if (_head == oldHead)
		{
			_head = oldHead->next;
			return true;
		}
		else
		{
			oldHead= _head;
			return false;
		}*/
		while (oldHead && _head.compare_exchange_weak(oldHead, oldHead->next) == false)
		{
			//oldHead = _head;
		};
		if(oldHead == nullptr)
		{
			--_popcount;
			return false;
		}

		// Exception X
		value = oldHead->data;
		TryDelete(oldHead);

		//잠시 삭제 보류
		//delete oldHead; // delete를 했는데 다른 thread에서 삭제한다면??

		// c#, java 같은 GC가 있으면 사실 여기서 끝
		// 누군가가 삭제를 하지 않을때 지우는 것이 핵심.

		return true;
	}

	// 1) 데이터 분리
	// 2) Count 체크
	// 3) 나 혼자면 삭제
	void TryDelete(Node* oldHead)
	{
		// 나 외에 누가 있는가?
		if(_popcount == 1)
		{
			// 나 혼자냐?

			// 이왕 혼자인거, 삭제 예약된 다른 데이터들도 삭제해 보자.
			Node* node = _pendingList.exchange(nullptr);
			if(--_popcount == 0)
			{
				// 끼어든 애가 없음 -> 삭제 진행
				// 이제 와서 끼어들어도, 어차피 데이터는 분리해둔 상태~!
				DeleteNode(node);				
			}
			else if (node)
			{
				// 누가 끼어들었으니, 다시 갔다 놓자.
				ChainPendingNodeList(node);				
			}

			// 내 데이터는 삭제
			delete oldHead;			
		}
		else 
		{
			// 누가 있네? 그럼 지금 삭제하지 않고 삭제 예약만
			ChainPendingNode(oldHead);
			--_popcount;
		}
	}

	// [] [][][][][] [] -> [ ] [ ][ ][ ]

	// [][][][][][][][]
	void ChainPendingNodeList(Node* first, Node* last)
	{
		// 혹시라도 중간에 끼어둘수 있다.
		last->next = _pendingList;

		while(_pendingList.compare_exchange_weak(last->next, first) == false)
		{			
		}
	}

	// [][][][][][][][][]
	void ChainPendingNodeList(Node* node)
	{
		Node* last = node;
		while (last->next)
			last = last->next;

		ChainPendingNodeList(node, last);		
	}
	void ChainPendingNode(Node* node)
	{
		ChainPendingNodeList(node);
	}

	// [][][][][][][][][][]
	static void DeleteNode(Node* node)
	{
		while(node)
		{
			Node* next = node->next;
			delete node;
			node = next;
		}
	}

private:
	// [][][][][][]
	// [head]
	atomic<Node*> _head;
	atomic<uint32> _popcount = 0; // Pop을 실행 중인 쓰레드 개수
	// [][][][][][][]
	atomic<Node*> _pendingList; // 삭제 되어야 할 노드들(첫번째 노드)
};
