#include "Button.h"
#include "Data/Button.h"
#include "NativeObject.h"
#include "View.h"
#include "QuickjsHelper.h"

namespace JSFunc_Button
{
static JSValue ctor(JSContext *ctx, JSValueConst new_target, int argc, JSValueConst *argv)
{
    JSValue obj = JS_EXCEPTION;
    if (argc == 0)
    {
        ::Button *nativeObject = new ::Button();
        obj = JS_NewObjectClass(ctx, jsbind::Button::classID);
        jsbind::NativeObject<::Button>::setNativeObjectPointer(ctx, obj, nativeObject, true);
    }
    else if (argc > 0)
    {
        if (jsbind::NativeObject<::Button>::canShareNativeObject(ctx, argv[0]))
        {
            obj = JS_NewObjectClass(ctx, jsbind::Button::classID);
            jsbind::NativeObject<::Button>::shareNativeObject(ctx, argv[0], obj);
        }
    }
    return obj;
}
static JSValue click(JSContext *ctx, JSValueConst thisVal, int argc, JSValueConst *argv)
{
    JSValue ret = JS_UNDEFINED;
    ::Button *nativeObject = jsbind::NativeObject<::Button>::getNativeObjectPointer(ctx, thisVal);
    nativeObject->click();
    return ret;
}
} // namespace JSFunc_Button

namespace jsbind
{
JSClassID Button::classID;
JSClassDef Button::classDef;
std::vector<JSCFunctionListEntry> Button::classProtoFuncs;

JSValue Button::import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef)
{
    Button::classDef.class_name = "Button";
    classProtoFuncs = jsbind::View::classProtoFuncs;
    classProtoFuncs.push_back(createMemberFunc("click", JSFunc_Button::click));

    JSValue constructor = importClass(ctx, Button::classID, Button::classDef, classProtoFuncs.data(),
                                      classProtoFuncs.size(), JSFunc_Button::ctor);
    JS_SetPropertyStr(ctx, obj, Button::classDef.class_name, constructor);

    NativeObject<::Button>::className = "@NativeButton";
    NativeObject<::Button>::import(ctx, obj, nullptr);

    return constructor;
}
} // namespace jsbind
