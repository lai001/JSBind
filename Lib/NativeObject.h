#ifndef NativeObject_H
#define NativeObject_H

#include <iostream>
#include <assert.h>

extern "C"
{
#include "quickjs.h"
}

namespace jsbind
{

JSValue importClass(JSContext *ctx, JSClassID &classID, JSClassDef &classDef, const JSCFunctionListEntry *tab,
                    const int len, JSCFunction *ctor);
JSCFunctionListEntry createMemberFunc(const char *name, JSCFunction cfunc);

JSCFunctionListEntry createMemberPropertyMagic(const char *name, decltype(JSCFunctionType::getter_magic) getter,
                                               decltype(JSCFunctionType::setter_magic) setter, const int16_t magic);

JSCFunctionListEntry createMemberProperty(const char *name, decltype(JSCFunctionType::getter) getter,
                                          decltype(JSCFunctionType::setter) setter);

} // namespace jsbind

namespace jsbind
{
template <typename> struct NativeObject;

template <typename TNativeClassType> struct NativeObjectHandle
{
    explicit NativeObjectHandle(TNativeClassType *ptr, const bool isManaged) : ptr(ptr), isManaged(isManaged)
    {
    }
    ~NativeObjectHandle()
    {
        if (isManaged && ptr)
        {
            std::cout << "Delete " << NativeObject<TNativeClassType>::className << "\n";
            delete ptr;
            ptr = nullptr;
        }
    }
    TNativeClassType *getPtr()
    {
        return ptr;
    }

  private:
    TNativeClassType *ptr;
    const bool isManaged;
};

template <typename TNativeClassType> struct NativeObject
{
    static JSClassID classID;
    static JSClassDef classDef;
    static const char *className;

    static JSValue ctor(JSContext *ctx, JSValueConst new_target, int argc, JSValueConst *argv)
    {
        JSValue obj = JS_EXCEPTION;
        return obj;
    }

    static void finalizer(JSRuntime *rt, JSValue val)
    {
        NativeObjectHandle<TNativeClassType> *nativeObject = reinterpret_cast<NativeObjectHandle<TNativeClassType> *>(
            JS_GetOpaque(val, NativeObject<TNativeClassType>::classID));
        if (nativeObject)
        {
            delete nativeObject;
        }
    }

    static JSValue import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef)
    {
        NativeObject<TNativeClassType>::classDef.class_name = NativeObject<TNativeClassType>::className;
        NativeObject<TNativeClassType>::classDef.finalizer = NativeObject<TNativeClassType>::finalizer;
        JSValue constructor =
            importClass(ctx, NativeObject<TNativeClassType>::classID, NativeObject<TNativeClassType>::classDef, nullptr,
                        0, NativeObject<TNativeClassType>::ctor);
        JS_SetPropertyStr(ctx, obj, NativeObject<TNativeClassType>::classDef.class_name, constructor);
        return constructor;
    }

    static JSValue ctorNative(JSContext *ctx, NativeObjectHandle<TNativeClassType> *nativeObject)
    {
        JSValue object = JS_NewObjectClass(ctx, NativeObject<TNativeClassType>::classID);
        JS_SetOpaque(object, nativeObject);
        return object;
    }

    static TNativeClassType *getNativeObjectPointer(JSContext *ctx, JSValueConst this_val)
    {
        NativeObjectHandle<TNativeClassType> *nativeObject;
        void *ppopaque;
        JSValue property = JS_GetPropertyStr(ctx, this_val, "@nativeObject");
        JS_GetClassID(property, &ppopaque);
        JS_FreeValue(ctx, property);
        nativeObject = reinterpret_cast<NativeObjectHandle<TNativeClassType> *>(ppopaque);
        return nativeObject->getPtr();
    }

    static void setNativeObjectPointer(JSContext *ctx, JSValueConst obj, TNativeClassType *nativeObject,
                                       const bool isManaged)
    {
        JSValue value = NativeObject<TNativeClassType>::ctorNative(
            ctx, new NativeObjectHandle<TNativeClassType>(nativeObject, isManaged));
        JS_SetPropertyStr(ctx, obj, "@nativeObject", value);
    }

    static void shareNativeObject(JSContext *ctx, JSValue sharedObj, JSValue to)
    {
        assert(canShareNativeObject(ctx, sharedObj));
        JSValue nativeObject = JS_GetPropertyStr(ctx, sharedObj, "@nativeObject");
        JS_SetPropertyStr(ctx, to, "@nativeObject", nativeObject);
    }

    static bool canShareNativeObject(JSContext *ctx, JSValue sharedObj)
    {
        if (JS_IsObject(sharedObj))
        {
            JSAtom atom = JS_NewAtom(ctx, "@nativeObject");
            const int flag = JS_HasProperty(ctx, sharedObj, atom);
            JS_FreeAtom(ctx, atom);
            return static_cast<bool>(flag);
        }
        return false;
    }
};

template <typename TNativeClassType> JSClassID NativeObject<TNativeClassType>::classID;

template <typename TNativeClassType> JSClassDef NativeObject<TNativeClassType>::classDef;

template <typename TNativeClassType> const char *NativeObject<TNativeClassType>::className;
} // namespace jsbind

#endif