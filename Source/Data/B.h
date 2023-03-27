#pragma once
#include <memory>
#include <string>
#include <vector>

class B : public std::enable_shared_from_this<B>
{
public:
    B();
    ~B();
    int data0;
    std::string str = "default value.";
    std::vector<int> cppvector;
};
