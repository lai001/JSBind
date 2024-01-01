#include "ImageView.h"
#include "Data/ImageView.h"
#include "NativeObject.h"
#include "View.h"

namespace JSFunc_ImageView
{
static JSValue ctor(JSContext *ctx, JSValueConst new_target, int argc, JSValueConst *argv)
{
    JSValue obj = JS_EXCEPTION;
    ::ImageView *nativeObject = new ::ImageView();
    obj = JS_NewObjectClass(ctx, jsbind::ImageView::classID);
    jsbind::NativeObject<::ImageView>::setNativeObjectPointer(ctx, obj, nativeObject, true);
    return obj;
}
static JSValue setImage(JSContext *ctx, JSValueConst thisVal, int argc, JSValueConst *argv)
{
    JSValue ret = JS_UNDEFINED;
    ::ImageView *nativeObject = jsbind::NativeObject<::ImageView>::getNativeObjectPointer(ctx, thisVal);
    nativeObject->setImage();
    return ret;
}
} // namespace JSFunc_ImageView

namespace jsbind
{
JSClassID ImageView::classID;
JSClassDef ImageView::classDef;
std::vector<JSCFunctionListEntry> ImageView::classProtoFuncs;

JSValue ImageView::import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef)
{
    ImageView::classDef.class_name = "ImageView";
    classProtoFuncs = jsbind::View::classProtoFuncs;
    classProtoFuncs.push_back(createMemberFunc("setImage", JSFunc_ImageView::setImage));

    JSValue constructor = importClass(ctx, ImageView::classID, ImageView::classDef, classProtoFuncs.data(),
                                      classProtoFuncs.size(), JSFunc_ImageView::ctor);
    JS_SetPropertyStr(ctx, obj, ImageView::classDef.class_name, constructor);

    NativeObject<::ImageView>::className = "@NativeImageView";
    NativeObject<::ImageView>::import(ctx, obj, nullptr);

    return constructor;
}
} // namespace jsbind
