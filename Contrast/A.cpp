#include "A.h"
#include "Data/A.h"
#include "NativeObject.h"

namespace JSFunc_A
{
static JSValue ctor(JSContext *ctx, JSValueConst new_target, int argc, JSValueConst *argv)
{
    JSValue obj = JS_EXCEPTION;
    if (argc > 1)
    {
        if (JS_IsNumber(argv[0]) && JS_IsNumber(argv[1]))
        {
            int argv0;
            int argv1;
            JS_ToInt32(ctx, &argv0, argv[0]);
            JS_ToInt32(ctx, &argv1, argv[1]);
            A *nativeObject = new A(argv0, argv1);
            obj = JS_NewObjectClass(ctx, jsbind::A::classID);
            jsbind::NativeObject<A>::setNativeObjectPointer(ctx, obj, nativeObject, true);
        }
    }
    return obj;
}

static JSValue mf_const(JSContext *ctx, JSValueConst this_val, int argc, JSValueConst *argv)
{
    JSValue ret = JS_EXCEPTION;
    A *nativeObject = jsbind::NativeObject<A>::getNativeObjectPointer(ctx, this_val);
    if (nativeObject)
    {
        if (argc > 1)
        {
            if (JS_IsNumber(argv[0]) && JS_IsNumber(argv[1]))
            {
                double argv0;
                int argv1;
                JS_ToFloat64(ctx, &argv0, argv[0]);
                JS_ToInt32(ctx, &argv1, argv[1]);
                std::string ret0 = nativeObject->mf_const(argv0, argv1);
                ret = JS_NewString(ctx, ret0.c_str());
            }
        }
    }
    return ret;
}
} // namespace JSFunc_A

namespace JSPropertyGet_A
{
static JSValue v4(JSContext *ctx, JSValueConst this_val)
{
    JSValue ret = JS_EXCEPTION;
    A *nativeObject = jsbind::NativeObject<A>::getNativeObjectPointer(ctx, this_val);
    if (nativeObject)
    {
        return JS_NewInt32(ctx, nativeObject->v4);
    }
    return ret;
}

} // namespace JSPropertyGet_A

namespace JSPropertySet_A
{
static JSValue v4(JSContext *ctx, JSValueConst this_val, JSValue val)
{
    JSValue ret = JS_EXCEPTION;
    A *nativeObject = jsbind::NativeObject<A>::getNativeObjectPointer(ctx, this_val);
    if (nativeObject)
    {
        if (JS_IsNumber(val))
        {
            int value;
            JS_ToInt32(ctx, &value, val);
            nativeObject->v4 = static_cast<decltype(nativeObject->v4)>(value);
            return JS_UNDEFINED;
        }
    }
    return ret;
}
} // namespace JSPropertySet_A

namespace jsbind
{
JSClassID A::classID;
JSClassDef A::classDef;
std::vector<JSCFunctionListEntry> A::classProtoFuncs;

JSValue A::import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef)
{
    A::classDef.class_name = "A";

    classProtoFuncs.push_back(createMemberProperty("v4", JSPropertyGet_A::v4, JSPropertySet_A::v4));
    classProtoFuncs.push_back(createMemberFunc("mf_const", JSFunc_A::mf_const));

    JSValue constructor =
        importClass(ctx, A::classID, A::classDef, classProtoFuncs.data(), classProtoFuncs.size(), JSFunc_A::ctor);
    JS_SetPropertyStr(ctx, obj, A::classDef.class_name, constructor);

    NativeObject<::A>::className = "@NativeA";
    NativeObject<::A>::import(ctx, obj, nullptr);

    return constructor;
}
} // namespace jsbind
