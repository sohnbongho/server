#include <iostream>
#include <thread>

void foo(int a, double d)
{
    std::cout << "foo" << std::endl;
}

struct Machine
{
    void Run(int a, double d)
    {
        std::cout << "Machine" << std::endl;
    }
};
struct Work
{
    void operator()(int a, double b) const
    {
        std::cout << "Work" << std::endl;
    }
};
int main()
{
    Machine m;
    Work w;  //w(1, 3.4); // 함수객체
    std::thread t1(&foo, 1, 3.4);
    std::thread t2(&Machine::Run, &m, 1, 3.4);
    std::thread t3(w, 1, 3.4);
    std::thread t4([] { std::cout << "lambda" << std::endl; });
    t1.join();
    t2.join();
    t3.join();
    t4.join();
}

