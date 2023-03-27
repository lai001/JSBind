#pragma once
#include "B.h"
#include <memory>

class BB : public B, public std::enable_shared_from_this<BB>
{
public:
	BB();
	~BB();
	int data1;
};
