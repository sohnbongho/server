
#include "ShapeFactory.h"



int main() {    

    // 함수 포인터를 이용한 생성자 팩토리 사용
    auto instance = ShapeFactory::getInstance();
    const type_info& typeRect = typeid(Circle);

	auto rect = instance->createShape(typeid(Rectangle));
    rect->draw();

    auto circle = instance->createShape(typeid(Circle));
    circle->draw();

    return 0;
}
