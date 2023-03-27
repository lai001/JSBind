#pragma once
#include <memory>
#include <string>

#include "B.h"

struct A : public std::enable_shared_from_this<A>
{
public:
	A();

	A(int v4, int v6);

	~A();

	std::string mf_const(double const a, int b) const;

	std::string mf1(double const a, int b);

	void mf2(double const a, int b);

	void printB();

	virtual std::string getName() const;

	std::string v0;
	char v1;
	unsigned char v2;
	signed char v3;
	int v4;
	unsigned int v5;
	signed int v6;
	short int v7;
	unsigned short int v8;
	signed short int v9;
	float v10;
	double v11;

	B* b;
};
