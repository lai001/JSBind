namespace JSGenerator
{
    class HelperGenerator
    {
        public HelperGenerator()
        { 
        }

        private string GetHeaderFileContent()
        {
            return $@"
#pragma once

extern ""C""
{{
#include ""cutils.h""
#include ""quickjs-libc.h""
}}
#include <assert.h>

#undef verify
# ifdef NDEBUG
#define verify(expression) expression
#else
#define verify(expression) assert(expression)
#endif

enum class EMemoryHostType
{{
    JS,
    Cpp
}};

enum class EMemoryType
{{
    RawPtr,
    SharedPtr
}};

JSCFunctionListEntry js_cgetset_magic_def(const char* name, decltype(JSCFunctionType::getter_magic) getter, decltype(JSCFunctionType::setter_magic) setter, const int16_t magic);

JSCFunctionListEntry js_cfunc_def(const char* name, const uint8_t length, decltype(JSCFunctionType::generic) func1);

JSCFunctionListEntry js_cgetset_def(const char* name, decltype(JSCFunctionType::getter) getter, decltype(JSCFunctionType::setter) setter);

int SetModuleExportHelper(JSContext* ctx, JSModuleDef* def, const JSClassDef* class_def, JSClassID* class_id, JSCFunction* constructor_func, const int constructor_func_length, const JSCFunctionListEntry* entries, const int entries_length, JSValue* outObject);

JSValue NewObjectProtoClass(JSContext* ctx, JSClassID class_id, JSValueConst new_target, void* userData);";
        }

        private string GetSourceFileContent()
        {
            return $@"
#include ""_QuickjsHelper.h""

JSCFunctionListEntry js_cgetset_magic_def(const char* name, decltype(JSCFunctionType::getter_magic) getter,
                                          decltype(JSCFunctionType::setter_magic) setter, const int16_t magic)
{{
    assert(name);
    JSCFunctionListEntry entry;
    entry.name = name;
    entry.magic = magic;
    entry.prop_flags = JS_PROP_CONFIGURABLE;
    entry.def_type = JS_DEF_CGETSET_MAGIC;
    entry.u.getset.get.getter_magic = getter;
    entry.u.getset.set.setter_magic = setter;
    return entry;
}}

JSCFunctionListEntry js_cgetset_def(const char* name, decltype(JSCFunctionType::getter) getter,
                        decltype(JSCFunctionType::setter) setter)
{{
    assert(name);
    JSCFunctionListEntry entry;
    entry.name = name;
    entry.prop_flags = JS_PROP_CONFIGURABLE;
    entry.def_type = JS_DEF_CGETSET;
    entry.u.getset.get.getter = getter;
    entry.u.getset.set.setter = setter;
    return entry;
}}

JSCFunctionListEntry js_cfunc_def(const char* name, const uint8_t length, decltype(JSCFunctionType::generic) func1)
{{
    assert(name);
    JSCFunctionListEntry entry;
    entry.name = name;
    entry.magic = 0;
    entry.prop_flags = JS_PROP_WRITABLE | JS_PROP_CONFIGURABLE;
    entry.def_type = JS_DEF_CFUNC;
    entry.u.func.length = 0;
    entry.u.func.cfunc.generic = func1;
    entry.u.func.cproto = JS_CFUNC_generic;
    return entry;
}}

int SetModuleExportHelper(JSContext *ctx, JSModuleDef *def, const JSClassDef *class_def, JSClassID* class_id,
                          JSCFunction *constructor_func, const int constructor_func_length,
                          const JSCFunctionListEntry *entries, const int entries_length, JSValue* outObject)
{{
    assert(ctx);
    assert(def);
    assert(class_def);
    assert(constructor_func);
    JS_NewClassID(class_id);
    if (int ret = JS_NewClass(JS_GetRuntime(ctx), *class_id, class_def) < 0)
    {{
        return ret;
    }}
    JSValue object_proto = JS_NewObject(ctx);
    if (JS_IsException(object_proto))
    {{
        return -1;
    }}
    JS_SetPropertyFunctionList(ctx, object_proto, entries, entries_length);
    JSValue constructor_func_object = JS_NewCFunction2(ctx, constructor_func, class_def->class_name,
                                                       constructor_func_length, JS_CFUNC_constructor, 0);
    if (JS_IsException(constructor_func_object))
    {{
        return -1;
    }}
    JS_SetConstructor(ctx, constructor_func_object, object_proto);
    JS_SetClassProto(ctx, *class_id, object_proto);
    if (int ret = JS_SetModuleExport(ctx, def, class_def->class_name, constructor_func_object) < 0)
    {{
        return ret;
    }}
    else if (outObject)
    {{
        *outObject = object_proto;
    }}
    return 0;
}}

JSValue NewObjectProtoClass(JSContext* ctx, JSClassID class_id, JSValueConst new_target, void* userData)
{{
    assert(ctx);
    JSValue proto = JS_GetPropertyStr(ctx, new_target, ""prototype"");
    if (JS_IsException(proto))
    {{
        return proto;
    }}
    JSValue obj = JS_NewObjectProtoClass(ctx, proto, class_id);
    if (JS_IsException(obj))
    {{
        return obj;
    }}
    JS_FreeValue(ctx, proto);
    JS_SetOpaque(obj, userData);
    return obj;
}}
";
        }

        public void Save(string outputFolderPath)
        {
            System.IO.Directory.CreateDirectory(outputFolderPath);

            System.IO.File.WriteAllText(outputFolderPath + "/" + "_QuickjsHelper.h", GetHeaderFileContent());
            System.IO.File.WriteAllText(outputFolderPath + "/" + "_QuickjsHelper.cpp", GetSourceFileContent());
        }
    }
}
