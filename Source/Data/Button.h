#pragma once
#include "View.h"
#include <memory>
#include <string>

class Button : public View, public std::enable_shared_from_this<Button>
{
  public:
    Button();
    ~Button();
    virtual std::string getDescription() override;
    void click();
};
