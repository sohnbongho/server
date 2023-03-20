
#include "ShapeFactory.h"



int main() {    

    // 함수 포인터를 이용한 생성자 팩토리 사용
    auto instance = ShapeFactory::getInstance();

	auto rect = instance->createShape("Rectangle");
    rect->draw();

    auto circle = instance->createShape("Circle");
    circle->draw();

    return 0;
}
