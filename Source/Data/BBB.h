#pragma once
#include "BB.h"
#include <memory>

class BBB : public BB, public std::enable_shared_from_this<BBB>
{
public:
	BBB();
	~BBB();
	int data2;
};
