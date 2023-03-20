#pragma once
#include <functional>
#include <unordered_map>

#include "Shape.h"

// ���� ������ �Լ� Ÿ�� ����
using ShapeConstructor = std::function<Shape* ()>;

class ShapeFactory {
private:
    static ShapeFactory* instance; // ���� ��� ����
    std::unordered_map<std::string, ShapeConstructor> m_constructors;

	ShapeFactory()
    {        
        //// ���� ������ �Լ� ���
        registerShape("Rectangle", []() { return new Rectangle(); });        
        registerShape("Circle", []() { return new Circle(); });


    } // �����ڸ� private�� �����Ͽ� �ܺο��� ��ü ������ ����   

public:
    static ShapeFactory* getInstance() { // ���� ��� �Լ�
        if (!instance)
            instance = new ShapeFactory();
        return instance;
    }
    // ������ �Լ� ���
    void registerShape(const std::string& name, ShapeConstructor constructor) {
        m_constructors[name] = constructor;
    }
    Shape* createShape(const std::string& name)
	{
        auto it = m_constructors.find(name);
        if(it != m_constructors.end())
        {
            return (*it).second();
        }
        return  nullptr;
    }

    void printMessage() {
        std::cout << "Hello, Singleton!" << std::endl;
    }
};

ShapeFactory* ShapeFactory::instance = nullptr; // ���� ��� ���� �ʱ�ȭ
