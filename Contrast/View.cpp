#include "View.h"
#include "NativeObject.h"
#include "QuickjsHelper.h"

namespace JSFunc_View
{
static JSValue ctor(JSContext *ctx, JSValueConst new_target, int argc, JSValueConst *argv)
{
    JSValue obj = JS_EXCEPTION;
    if (argc == 0)
    {
        ::View *nativeObject = new ::View();
        obj = JS_NewObjectClass(ctx, jsbind::View::classID);
        jsbind::NativeObject<::View>::setNativeObjectPointer(ctx, obj, nativeObject, true);
    }
    else if (argc > 0)
    {
        if (jsbind::NativeObject<::View>::canShareNativeObject(ctx, argv[0]))
        {
            obj = JS_NewObjectClass(ctx, jsbind::View::classID);
            jsbind::NativeObject<::View>::shareNativeObject(ctx, argv[0], obj);
        }
    }
    return obj;
}

static JSValue getDescription(JSContext *ctx, JSValueConst thisVal, int argc, JSValueConst *argv)
{
    JSValue ret = JS_EXCEPTION;
    ::View *nativeObject = jsbind::NativeObject<::View>::getNativeObjectPointer(ctx, thisVal);
    const std::string description = nativeObject->getDescription();
    ret = JS_NewString(ctx, description.c_str());
    return ret;
}

} // namespace JSFunc_View

namespace jsbind
{
JSClassID View::classID;
JSClassDef View::classDef;
std::vector<JSCFunctionListEntry> View::classProtoFuncs;

JSValue View::import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef)
{
    View::classDef.class_name = "View";
    classProtoFuncs.push_back(createMemberFunc("getDescription", JSFunc_View::getDescription));

    JSValue constructor = importClass(ctx, View::classID, View::classDef, classProtoFuncs.data(),
                                      classProtoFuncs.size(), JSFunc_View::ctor);
    JS_SetPropertyStr(ctx, obj, View::classDef.class_name, constructor);

    NativeObject<::View>::className = "@NativeView";
    NativeObject<::View>::import(ctx, obj, nullptr);

    return constructor;
}
JSValue View::setNativeObjectPointer(JSContext *ctx, ::View *nativeObject, const bool isManaged)
{
    assert(JS_IsRegisteredClass(JS_GetRuntime(ctx), jsbind::View::classID));
    JSValue ret = JS_NewObjectClass(ctx, jsbind::View::classID);
    jsbind::NativeObject<::View>::setNativeObjectPointer(ctx, ret, nativeObject, isManaged);
    return ret;
}
} // namespace jsbind
