#include "B.h"
#include <spdlog/spdlog.h>

B::B()
    : data0(100)
{
    cppvector.push_back(0);
}

B::~B()
{
    //spdlog::debug("~B()");
}
