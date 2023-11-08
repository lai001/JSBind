#include "NativeObject.h"

namespace jsbind
{
JSCFunctionListEntry createMemberFunc(const char *name, JSCFunction cfunc)
{
    JSCFunctionListEntry entry = JSCFunctionListEntry();
    entry.name = name;
    entry.prop_flags = JS_PROP_WRITABLE | JS_PROP_CONFIGURABLE;
    entry.def_type = JS_DEF_CFUNC;
    entry.magic = 0;
    entry.u.func.length = 0;
    entry.u.func.cproto = JS_CFUNC_generic;
    entry.u.func.cfunc.generic = cfunc;
    return entry;
}

JSCFunctionListEntry createMemberPropertyMagic(const char *name, decltype(JSCFunctionType::getter_magic) getter,
                                               decltype(JSCFunctionType::setter_magic) setter, const int16_t magic)
{
    JSCFunctionListEntry entry = JSCFunctionListEntry();
    entry.name = name;
    entry.magic = magic;
    entry.prop_flags = JS_PROP_CONFIGURABLE;
    entry.def_type = JS_DEF_CGETSET_MAGIC;
    entry.u.getset.get.getter_magic = getter;
    entry.u.getset.set.setter_magic = setter;
    return entry;
}

JSCFunctionListEntry createMemberProperty(const char *name, decltype(JSCFunctionType::getter) getter,
                                          decltype(JSCFunctionType::setter) setter)
{
    JSCFunctionListEntry entry = JSCFunctionListEntry();
    entry.name = name;
    entry.prop_flags = JS_PROP_CONFIGURABLE;
    entry.def_type = JS_DEF_CGETSET;
    entry.u.getset.get.getter = getter;
    entry.u.getset.set.setter = setter;
    return entry;
}

JSValue importClass(JSContext *ctx, JSClassID &classID, JSClassDef &classDef, const JSCFunctionListEntry *tab,
                    const int len, JSCFunction *ctor)
{
    JS_NewClassID(&classID);
    JS_NewClass(JS_GetRuntime(ctx), classID, &classDef);
    JSValue proto = JS_NewObject(ctx);
    JS_SetPropertyFunctionList(ctx, proto, tab, len);
    JSValue constructor = JS_NewCFunction2(ctx, ctor, classDef.class_name, 2, JS_CFUNC_constructor, 0);
    JS_SetConstructor(ctx, constructor, proto);
    JS_SetClassProto(ctx, classID, proto);
    return constructor;
}
} // namespace jsbind
