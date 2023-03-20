#include "Person.h"

#include <string>

Person::Person(const std::string& n, int a) : _name{ n }, _age{a}
{    
}
void Person::setAge(int age)
{
    _age = age;
}

int Person::GetAge() const
{
	return _age;
}
