#pragma once
#include <string>

class Person
{
    std::string _name;
    int _age;
public:
    Person(const std::string& n, int a);
    void setAge(int age);
    int GetAge() const;
};
