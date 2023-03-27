#include "BB.h"
#include <spdlog/spdlog.h>

BB::BB()
	:B(), data1(200)
{
	str = "BB: " + str;
}

BB::~BB()
{
	//spdlog::debug("~BB()");
}
