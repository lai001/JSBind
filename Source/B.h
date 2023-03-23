#pragma once
#include <memory>
#include <string>

struct B : public std::enable_shared_from_this<B>
{
    B();
    ~B();

    int data;
    std::string str = "default value.";
};
