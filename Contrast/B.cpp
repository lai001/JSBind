#include "B.h"
#include "Data/B.h"
#include "NativeObject.h"

namespace JSFunc_B
{
static JSValue ctor(JSContext *ctx, JSValueConst new_target, int argc, JSValueConst *argv)
{
    JSValue obj = JS_EXCEPTION;
    if (argc == 0)
    {
        ::B *nativeObject = new ::B();
        obj = JS_NewObjectClass(ctx, jsbind::B::classID);
        jsbind::NativeObject<::B>::setNativeObjectPointer(ctx, obj, nativeObject, true);
    }
    else if (argc > 0)
    {
        if (jsbind::NativeObject<::B>::canShareNativeObject(ctx, argv[0]))
        {
            obj = JS_NewObjectClass(ctx, jsbind::B::classID);
            jsbind::NativeObject<::B>::shareNativeObject(ctx, argv[0], obj);
        }
    }
    return obj;
}
} // namespace JSFunc_B

namespace JSPropertyGet_B
{
static JSValue data0(JSContext *ctx, JSValueConst this_val)
{
    JSValue ret = JS_EXCEPTION;
    ::B *nativeObject = jsbind::NativeObject<::B>::getNativeObjectPointer(ctx, this_val);
    if (nativeObject)
    {
        return JS_NewInt32(ctx, nativeObject->data0);
    }
    return ret;
}

} // namespace JSPropertyGet_B

namespace JSPropertySet_B
{
static JSValue data0(JSContext *ctx, JSValueConst this_val, JSValue val)
{
    JSValue ret = JS_EXCEPTION;
    ::B *nativeObject = jsbind::NativeObject<::B>::getNativeObjectPointer(ctx, this_val);
    if (nativeObject)
    {
        if (JS_IsNumber(val))
        {
            int value;
            JS_ToInt32(ctx, &value, val);
            nativeObject->data0 = static_cast<decltype(nativeObject->data0)>(value);
            return JS_UNDEFINED;
        }
    }
    return ret;
}
} // namespace JSPropertySet_B

namespace jsbind
{
JSClassID B::classID;
JSClassDef B::classDef;
std::vector<JSCFunctionListEntry> B::classProtoFuncs;

JSValue B::import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef)
{
    B::classDef.class_name = "B";

    classProtoFuncs.push_back(createMemberProperty("data0", JSPropertyGet_B::data0, JSPropertySet_B::data0));

    JSValue constructor =
        importClass(ctx, B::classID, B::classDef, classProtoFuncs.data(), classProtoFuncs.size(), JSFunc_B::ctor);
    JS_SetPropertyStr(ctx, obj, B::classDef.class_name, constructor);

    NativeObject<::B>::className = "@NativeB";
    NativeObject<::B>::import(ctx, obj, nullptr);

    return constructor;
}
} // namespace jsbind
