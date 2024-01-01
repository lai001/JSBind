#pragma once
#include <memory>
#include <string>

class View : public std::enable_shared_from_this<View>
{
  public:
    View();
    ~View();
    virtual std::string getDescription();
};
