#pragma once
#include "CoreMacro.h"
#include "Allocator.h"

template<typename Type, typename ... Args>
Type* xnew(Args&&... args)
{
	Type* memory = static_cast<Type*>(xxalloc(sizeof(Type)));

	// placement new 문법 (생성자 호출)
	new(memory)Type(std::forward<Args>(args)...);
	return memory;
}

template<typename Type>
void xdelete(Type* obj)
{
	obj->~Type();
	xxrelease(obj);
}