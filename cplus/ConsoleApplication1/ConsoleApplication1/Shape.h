#pragma once
#include <iostream>

class Shape
{
public:
	virtual void draw() = 0;
};

// Circle Ŭ����
class Circle : public Shape {
public:
    void draw() {
        std::cout << "Circle::draw() called" << std::endl;
    }
};

// Rectangle Ŭ����
class Rectangle : public Shape {
public:
    void draw() {
        std::cout << "Rectangle::draw() called" << std::endl;
    }
};
