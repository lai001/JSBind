using CppSharp.AST;
using System;

namespace JSGenerator
{
    class CPPVectorRegisterGenerator : IRegister
    {
        private TemplateSpecializationType templateSpecializationType;

        public CPPVectorRegisterGenerator(TemplateSpecializationType templateSpecializationType)
        {
            this.templateSpecializationType = templateSpecializationType;
        }

        public static string getIncludeFileName(TemplateSpecializationType templateSpecializationType)
        {
            string typeName = templateSpecializationType.Arguments[0].ToString();
            return getHeaderFileName(typeName);
        }

        static string getHeaderFileName(string typeName)
        {
            return $@"ClassCPPVectorRegister_{typeName}.h";
        }

        string getSourceFileName(string typeName)
        {
            return $@"ClassCPPVectorRegister_{typeName}.cpp";
        }

        string getHeaderFileContent()
        {
            TemplateArgument templateArgument = templateSpecializationType.Arguments[0];
            string typeName = templateArgument.Type.ToString();

            string ret = $@"
#pragma once
#include ""CPPVector_{typeName}.h""
#include ""_QuickjsHelper.h""

struct JSWrapperCPPVector_{typeName}
{{      
    EMemoryHostType HostType = EMemoryHostType::JS;
    EMemoryType MemoryType = EMemoryType::RawPtr;
    CPPVector_{typeName}* instance = nullptr;

    static inline JSWrapperCPPVector_{typeName} *UnretainedSetOpaque(JSValue objectClass)
    {{
        JSWrapperCPPVector_{typeName}* wrapper = new JSWrapperCPPVector_{typeName}();
        JS_SetOpaque(objectClass, wrapper);
        return wrapper;
    }}
}};
JSClassID get_js_CPPVector_{typeName}_class_id();

";
            return ret;
        }

        string getSourceFileContent()
        {
            TemplateArgument templateArgument = templateSpecializationType.Arguments[0];
            string typeName = templateArgument.Type.ToString();

            string ret = $@"
#include ""ClassCPPVectorRegister_{typeName}.h""
static JSClassID js_CPPVector_{typeName}_class_id;

JSClassID get_js_CPPVector_{typeName}_class_id()
{{
    return js_CPPVector_{typeName}_class_id;
}}

static void js_CPPVector_{typeName}_finalizer(JSRuntime *rt, JSValue val)
{{
    if (JSWrapperCPPVector_{typeName} *wrapper =
            reinterpret_cast<JSWrapperCPPVector_{typeName} *>(JS_GetOpaque(val, JS_GetClassID(val, nullptr))))
    {{
        if (wrapper->HostType == EMemoryHostType::JS)
        {{
            delete wrapper->instance;
        }}
        delete wrapper;
    }}
}}

static JSValue js_CPPVector_{typeName}_ctor(JSContext *ctx, JSValueConst new_target, int argc, JSValueConst *argv)
{{
    CPPVector_{typeName} *instance = nullptr;

    if (argc == 0)
    {{
        instance = new CPPVector_{typeName}();
    }}
    JSWrapperCPPVector_{typeName} *wrapper = new JSWrapperCPPVector_{typeName}();
    wrapper->instance = instance;
    assert(instance);
    return NewObjectProtoClass(ctx, js_CPPVector_{typeName}_class_id, new_target, wrapper);
}}

struct JSCPPVector_{typeName}MemberFunction
{{

    static JSValue set(JSContext *ctx, JSValueConst this_val, int argc, JSValueConst *argv)
    {{
        JSWrapperCPPVector_{typeName} *wrapper =
            reinterpret_cast<JSWrapperCPPVector_{typeName} *>(JS_GetOpaque2(ctx, this_val, JS_GetClassID(this_val, nullptr)));
        CPPVector_{typeName} *instance = wrapper->instance;
        assert(instance);
        assert(argc == 2);

        int64_t v0;
        verify(JS_ToInt64(ctx, &v0, argv[0]) >= 0);

        {typeName} v1;
        verify(JS_ToInt32(ctx, &v1, argv[1]) >= 0);

        instance->set(v0, v1);
        return JS_UNDEFINED;
    }}

    static JSValue get(JSContext *ctx, JSValueConst this_val, int argc, JSValueConst *argv)
    {{

        JSWrapperCPPVector_{typeName} *wrapper =
            reinterpret_cast<JSWrapperCPPVector_{typeName} *>(JS_GetOpaque2(ctx, this_val, JS_GetClassID(this_val, nullptr)));
        CPPVector_{typeName} *instance = wrapper->instance;
        assert(instance);
        assert(argc == 1);

        int64_t v0;
        verify(JS_ToInt64(ctx, &v0, argv[0]) >= 0);

        {typeName} r = instance->at(v0);
        return JS_NewInt32(ctx, r);
    }}
}};

static JSClassDef *js_CPPVector_{typeName}_class()
{{
    static JSClassDef class_def;
    class_def.class_name = ""CPPVector_{typeName}"";
    class_def.finalizer = js_CPPVector_{typeName}_finalizer;
    return &class_def;
}}

int js_CPPVector_{typeName}_SetModuleExport(JSContext *ctx, JSModuleDef *def)
{{
    static JSCFunctionListEntry js_class_proto_funcs[] = {{
        js_cfunc_def(""set"", 0, JSCPPVector_{typeName}MemberFunction::set),
        js_cfunc_def(""get"", 0, JSCPPVector_{typeName}MemberFunction::get),
    }};
    JSValue object;
    SetModuleExportHelper(ctx, def, js_CPPVector_{typeName}_class(), &js_CPPVector_{typeName}_class_id, js_CPPVector_{typeName}_ctor, 2,
                          js_class_proto_funcs, sizeof(js_class_proto_funcs) / sizeof((js_class_proto_funcs)[0]),
                          &object);
    return 0;
}}

int js_CPPVector_{typeName}_AddModuleExport(JSContext *ctx, JSModuleDef *def)
{{
    return JS_AddModuleExport(ctx, def, js_CPPVector_{typeName}_class()->class_name);
}}

";
            return ret;
        }

        public void save(string outputFolderPath)
        {
            System.IO.Directory.CreateDirectory(outputFolderPath);
            string typeName = templateSpecializationType.Arguments[0].ToString();
            System.IO.File.WriteAllText(outputFolderPath + "/" + getHeaderFileName(typeName), getHeaderFileContent());
            System.IO.File.WriteAllText(outputFolderPath + "/" + getSourceFileName(typeName), getSourceFileContent());
        }

        public Tuple<string, string, string> getRegisterClassCallerContent()
        {
            TemplateArgument templateArgument = templateSpecializationType.Arguments[0];
            string typeName = templateArgument.Type.ToString();

            Tuple<string, string, string> tuple = new Tuple<string, string, string>($@"
extern int js_CPPVector_{typeName}_SetModuleExport(JSContext *ctx, JSModuleDef *def);
extern int js_CPPVector_{typeName}_AddModuleExport(JSContext *ctx, JSModuleDef *def);",
$@"js_CPPVector_{typeName}_SetModuleExport(ctx, def);",
$@"js_CPPVector_{typeName}_AddModuleExport(ctx, def);");
            return tuple;
        }
    }
}
