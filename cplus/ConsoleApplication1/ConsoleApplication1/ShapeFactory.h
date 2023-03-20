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
        registerShape(typeid(Rectangle), []() { return new Rectangle(); });
        registerShape(typeid(Circle), []() { return new Circle(); });


    } // �����ڸ� private�� �����Ͽ� �ܺο��� ��ü ������ ����   

public:
    static ShapeFactory* getInstance() { // ���� ��� �Լ�
        if (!instance)
            instance = new ShapeFactory();
        return instance;
    }
    // ������ �Լ� ���
    void registerShape(const type_info& typeId, ShapeConstructor constructor) {
        m_constructors[typeId.name()] = constructor;
    }
    Shape* createShape(const type_info& typeId)
	{
        auto it = m_constructors.find(typeId.name());
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
