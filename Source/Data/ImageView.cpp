#include "ImageView.h"
#include <spdlog/spdlog.h>

ImageView::ImageView()
	:View()
{
}

ImageView::~ImageView()
{

}

std::string ImageView::getDescription()
{
    return std::string("This is a image view.");
}

void ImageView::setImage()
{
    spdlog::debug("Set image.");
}
