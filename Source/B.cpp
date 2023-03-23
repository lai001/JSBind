#include "B.h"
#include <spdlog/spdlog.h>

B::B()
    : data(0)
{
}

B::~B()
{
    spdlog::debug("~B()");
}
