#pragma once
#include <functional>
#include <unordered_map>

#include "Shape.h"

// 도형 생성자 함수 타입 정의
using ShapeConstructor = std::function<Shape* ()>;

class ShapeFactory {
private:
    static ShapeFactory* instance; // 정적 멤버 변수
    std::unordered_map<std::string, ShapeConstructor> m_constructors;

	ShapeFactory()
    {        
        //// 도형 생성자 함수 등록
        registerShape(typeid(Rectangle), []() { return new Rectangle(); });
        registerShape(typeid(Circle), []() { return new Circle(); });


    } // 생성자를 private로 선언하여 외부에서 객체 생성을 막음   

public:
    static ShapeFactory* getInstance() { // 정적 멤버 함수
        if (!instance)
            instance = new ShapeFactory();
        return instance;
    }
    // 생성자 함수 등록
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

ShapeFactory* ShapeFactory::instance = nullptr; // 정적 멤버 변수 초기화
