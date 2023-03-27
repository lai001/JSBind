#include "BBB.h"
#include <spdlog/spdlog.h>

BBB::BBB()
	:BB(), data2(300)
{
	str = "BBB: " + str;
}

BBB::~BBB()
{
	//spdlog::debug("~BBB()");
}
