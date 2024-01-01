#include "Button.h"
#include <spdlog/spdlog.h>

Button::Button() : View()
{
}

Button::~Button()
{
}

std::string Button::getDescription()
{
    return std::string("This is a button.");
}

void Button::click()
{
    spdlog::debug("Click.");
}
