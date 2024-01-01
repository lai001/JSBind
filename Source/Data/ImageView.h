#pragma once
#include <string>
#include <memory>
#include "View.h"

class ImageView : public View, public std::enable_shared_from_this<ImageView>
{
  public:
    ImageView();
    ~ImageView();
    virtual std::string getDescription() override;
    void setImage();
};
