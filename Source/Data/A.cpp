#include "A.h"
#include <spdlog/spdlog.h>

A::A()
{
}

A::A(int v4, int v6)
    : v4(v4), v6(v6), b(nullptr)
{
}

A::~A()
{
    //spdlog::debug("~A()");
}

std::string A::mf_const(double const a, int b) const
{
    return std::string("std::string A::mf_const(double const a, int b) const");
}

std::string A::mf1(double const a, int b)
{
    return std::string("std::string A::mf1(double const a, int b)");
}

void A::mf2(double const a, int b)
{
    spdlog::debug("void A::mf2(double const a, int b)");
}

void A::printB()
{
    if (b)
    {
        spdlog::debug("{}", b->data0);
    }
    else
    {
        spdlog::debug("b is nullptr.");
    }
}

std::string A::getName() const
{
    return std::string("A");
}