#include "BB.h"
#include "Data/BB.h"
#include "NativeObject.h"
#include "B.h"

namespace JSFunc_BB
{
static JSValue ctor(JSContext *ctx, JSValueConst new_target, int argc, JSValueConst *argv)
{
    JSValue obj = JS_EXCEPTION;
    ::BB *nativeObject = new ::BB();
    obj = JS_NewObjectClass(ctx, jsbind::BB::classID);
    jsbind::NativeObject<::BB>::setNativeObjectPointer(ctx, obj, nativeObject, true);
    return obj;
}
} // namespace JSFunc_BB

namespace JSPropertyGet_BB
{
static JSValue data1(JSContext *ctx, JSValueConst this_val)
{
    JSValue ret = JS_EXCEPTION;
    ::BB *nativeObject = jsbind::NativeObject<::BB>::getNativeObjectPointer(ctx, this_val);
    if (nativeObject)
    {
        return JS_NewInt32(ctx, nativeObject->data1);
    }
    return ret;
}

} // namespace JSPropertyGet_BB

namespace JSPropertySet_BB
{
static JSValue data1(JSContext *ctx, JSValueConst this_val, JSValue val)
{
    JSValue ret = JS_EXCEPTION;
    ::BB *nativeObject = jsbind::NativeObject<::BB>::getNativeObjectPointer(ctx, this_val);
    if (nativeObject)
    {
        if (JS_IsNumber(val))
        {
            int value;
            JS_ToInt32(ctx, &value, val);
            nativeObject->data1 = static_cast<decltype(nativeObject->data1)>(value);
            return JS_UNDEFINED;
        }
    }
    return ret;
}
} // namespace JSPropertySet_BB

namespace jsbind
{
JSClassID BB::classID;
JSClassDef BB::classDef;
std::vector<JSCFunctionListEntry> BB::classProtoFuncs;

JSValue BB::import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef)
{
    BB::classDef.class_name = "BB";
    classProtoFuncs = jsbind::B::classProtoFuncs;
    classProtoFuncs.push_back(createMemberProperty("data1", JSPropertyGet_BB::data1, JSPropertySet_BB::data1));

    JSValue constructor =
        importClass(ctx, BB::classID, BB::classDef, classProtoFuncs.data(), classProtoFuncs.size(), JSFunc_BB::ctor);
    JS_SetPropertyStr(ctx, obj, BB::classDef.class_name, constructor);

    NativeObject<::BB>::className = "@NativeBB";
    NativeObject<::BB>::import(ctx, obj, nullptr);

    return constructor;
}
} // namespace jsbind
