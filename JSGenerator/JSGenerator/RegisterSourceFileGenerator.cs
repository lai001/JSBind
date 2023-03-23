using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{
    class RegisterSourceFileGenerator
    {
        private ASTContext ctx;
        private Class @class;
        private string outputPath;

        public RegisterSourceFileGenerator(ASTContext ctx, Class @class, string outputPath)
        {
            this.outputPath = outputPath;
            this.@class = @class;
            this.ctx = ctx;
        }

        public string getSourceFileContent()
        {
            string className = @class.Name;
            Func<string> retrieveInstance = delegate
            {
                return $@"
JSWrapper{className}* wrapper = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque2(ctx, this_val, js_{className}_class_id));
{className}* instance = wrapper->instance;";
            };
            string ret = @$"
{getIncludeContent()}
static JSClassID js_{className}_class_id;

JSClassID get_js_{className}_class_id()
{{
    return js_{className}_class_id;
}}

{getFinalizerContent()}
{getCtorContent()}
{MemberFunctionGenerator.get(@class, getSupportMemberMethod(), retrieveInstance, null)}
{getGetPropContent()}
{getSetPropContent()}
{getClassDefContent()}
{getClassSetModuleExportContent()}
{getClassAddModuleExportContent()}
";
            return ret;
        }

        public string getIncludeContent()
        {
            string className = @class.Name;
            string headerFilePath = @class.TranslationUnit.IncludePath;
            string ret = @$"
#include ""Class{className}Register.h""";
            return ret;
        }

        public string getCtorContent()
        {
            string className = @class.Name;
            string c = "";
            List<Method> methods = getSupportContructorMethod();

            for (int i = 0; i < methods.Count; i++)
            {
                Method method = methods[i];
                if (method.Parameters.Count > 0)
                {
                    string parametersCodeLine = "";
                    for (int parameterIndex = 0; parameterIndex < method.Parameters.Count; parameterIndex++)
                    {
                        Parameter parameter = method.Parameters[parameterIndex];
                        parametersCodeLine += MemberFunctionGenerator.getParameterContent(parameter, parameterIndex);
                    }

                    string vlist = MemberFunctionGenerator.getVlist(method.Parameters.Count);
                    c += $@"
if (argc == {method.Parameters.Count})
{{
    {parametersCodeLine}
    instance = new {className}({vlist});
}}";
                }
                else
                {
                    c += $@"
if (argc == {method.Parameters.Count})
{{
    instance = new {className}();
}}";
                }
            }

            string ret = @$"
static JSValue js_{className}_ctor(JSContext* ctx, JSValueConst new_target, int argc, JSValueConst* argv)
{{
    {className}* instance = nullptr;
    {c}
	JSWrapper{className}* wrapper = new JSWrapper{className}();
	wrapper->instance = instance;
    assert(instance);
	return NewObjectProtoClass(ctx, js_{className}_class_id, new_target, wrapper);
}}";
            return ret;
        }

        public string getFinalizerContent()
        {
            string className = @class.Name;
            string ret = $@"
static void js_{className}_finalizer(JSRuntime* rt, JSValue val)
{{
	if (JSWrapper{className}* wrapper = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque(val, js_{className}_class_id)))
	{{
		if (wrapper->HostType == EMemoryHostType::JS)
		{{
            delete wrapper->instance;
		}}
		delete wrapper;
	}}
}}";
            return ret;
        }

        public string getClassDefContent()
        {
            string className = @class.Name;
            string ret = $@"
static JSClassDef* js_{className}_class()
{{
	static JSClassDef class_def;
	class_def.class_name = ""{className}"";
	class_def.finalizer = js_{className}_finalizer;
	return &class_def;
}}";
            return ret;
        }

        public string getClassAddModuleExportContent()
        {
            string className = @class.Name;
            string ret = $@"
int js_{className}_AddModuleExport(JSContext* ctx, JSModuleDef* def)
{{
	return JS_AddModuleExport(ctx, def, js_{className}_class()->class_name);
}}";
            return ret;
        }

        public string getClassSetModuleExportContent()
        {
            string className = @class.Name;
            string propFunc = "";
            string memberFunc = "";
            for (int i = 0; i < @class.Fields.Count; i++)
            {
                Field field = @class.Fields[i];
                string name = field.LogicalOriginalName;
                propFunc += $@"js_cgetset_magic_def(""{name}"", js_{className}_get_prop, js_{className}_set_prop, {i})," + "\n";
            }

            List<Method> supportMemberMethods = getSupportMemberMethod();
            for (int i = 0; i < supportMemberMethods.Count; i++)
            {
                Method method = supportMemberMethods[i];
                string name = method.LogicalOriginalName;
                memberFunc += $@"js_cfunc_def(""{name}"", 0, JS{className}MemberFunction::{name})," + "\n";
            }
            string content = "";
            if (supportMemberMethods.Count + @class.Fields.Count > 0)
            {
                content = $@"
static JSCFunctionListEntry js_class_proto_funcs[] = {{
    {propFunc}
    {memberFunc}
}};
SetModuleExportHelper(ctx, def, js_{className}_class(), &js_{className}_class_id, js_{className}_ctor, 2, js_class_proto_funcs, sizeof(js_class_proto_funcs) / sizeof((js_class_proto_funcs)[0]));
";
            }
            else
            {
                content = $@"SetModuleExportHelper(ctx, def, js_{className}_class(), js_{className}_class_id, js_{className}_ctor, 2, nullptr, 0);";
            }

            string ret = $@"
int js_{className}_SetModuleExport(JSContext* ctx, JSModuleDef* def)
{{
	{content}
	return 0;
}}";
            return ret;
        }

        public string getGetPropContent()
        {
            string className = @class.Name;
            string content = "";
            for (int i = 0; i < @class.Fields.Count; i++)
            {
                Field field = @class.Fields[i];
                content += getGetPropMagicContent(field, i) + "\n";
            }

            string ret = $@"
static JSValue js_{className}_get_prop(JSContext* ctx, JSValueConst this_val, int magic)
{{
	JSWrapper{className}* wrapper = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque2(ctx, this_val, js_{className}_class_id));
	{className}* instance = wrapper->instance;
    {content}
	return JS_EXCEPTION;
}}";

            return ret;
        }

        private static Dictionary<string, string> getNewTypeMap()
        {
            Dictionary<string, string> typeMap = new Dictionary<string, string>();
            typeMap["std::string"] = "JS_NewString";
            typeMap["sbyte"] = "JS_NewInt32";
            typeMap["byte"] = "JS_NewInt32";
            typeMap["sbyte"] = "JS_NewInt32";
            typeMap["uint"] = "JS_NewInt32";
            typeMap["int"] = "JS_NewInt32";
            typeMap["short"] = "JS_NewInt32";
            typeMap["ushort"] = "JS_NewInt32";
            typeMap["float"] = "JS_NewFloat64";
            typeMap["double"] = "JS_NewFloat64";
            return typeMap;
        }

        private static Dictionary<string, string> getToTypeMap()
        {
            Dictionary<string, string> typeMap = new Dictionary<string, string>();
            typeMap["std::string"] = "JS_ToString";
            typeMap["sbyte"] = "JS_ToInt32";
            typeMap["byte"] = "JS_ToInt32";
            typeMap["sbyte"] = "JS_ToInt32";
            typeMap["uint"] = "JS_ToInt32";
            typeMap["int"] = "JS_ToInt32";
            typeMap["short"] = "JS_ToInt32";
            typeMap["ushort"] = "JS_ToInt32";
            typeMap["float"] = "JS_ToFloat64";
            typeMap["double"] = "JS_ToFloat64";
            return typeMap;
        }

        private static Dictionary<string, string> getTypeDMap()
        {
            Dictionary<string, string> typeMap = new Dictionary<string, string>();
            typeMap["sbyte"] = "int";
            typeMap["byte"] = "int";
            typeMap["sbyte"] = "int";
            typeMap["uint"] = "int";
            typeMap["int"] = "int";
            typeMap["short"] = "int";
            typeMap["ushort"] = "int";
            typeMap["float"] = "double";
            typeMap["double"] = "double";
            return typeMap;
        }

        private static string getNamespaceFieldType(Field field)
        {
            string @namespace = "";
            if (field.Type is TypedefType)
            {
                TypedefType typedefType = field.Type as TypedefType;
                @namespace = typedefType.Declaration.Namespace.ToString();
            }
            else if (field.Type is PointerType)
            {
                PointerType pointerType = field.Type as PointerType;
            }
            string fieldType = field.Type.ToString();
            if (@namespace.Length > 0)
            {
                return $@"{@namespace}::{fieldType}";
            }
            else
            {
                return fieldType;
            }
        }

        public string getGetPropMagicContent(Field field, int magic)
        {
            string content = "";
            if (field.Type is PointerType)
            {
                content = $@"return JS_GetPropertyStr(ctx, this_val, ""{magic}{field.LogicalOriginalName}"");";
            }
            else
            {
                Dictionary<string, string> typeMap = getNewTypeMap();
                string fieldType = getNamespaceFieldType(field);
                string jsFunc = "";
                string fix = "";
                if (fieldType == "std::string")
                {
                    fix = ".c_str()";
                }
                if (typeMap.ContainsKey(fieldType))
                {
                    jsFunc = typeMap[fieldType];
                }
                content = $@"return {jsFunc}(ctx, instance->{field.LogicalOriginalName}{fix});";
            }

            string elseif = magic == 0 ? "if" : "else if";
            string ret = $@"
{elseif} (magic == {magic})
{{
    {content}
}}";
            return ret;
        }

        public string getSetPropContent()
        {
            string className = @class.Name;
            string content = "";
            for (int i = 0; i < @class.Fields.Count; i++)
            {
                Field field = @class.Fields[i];
                content += getSetPropMagicContent(field, i) + "\n";
            }

            string ret = $@"
static JSValue js_{className}_set_prop(JSContext* ctx, JSValueConst this_val, JSValue val, int magic)
{{
	JSWrapper{className}* wrapper = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque2(ctx, this_val, js_{className}_class_id));
	{className}* instance = wrapper->instance;
    {content}
	return JS_EXCEPTION;
}}";

            return ret;
        }

        public string getSetPropMagicContent(Field field, int magic)
        {
            string content = "";

            if (field.Type is PointerType)
            {
                PointerType pointerType = field.Type as PointerType;
                string className = (pointerType.Pointee as TagType).Declaration.Name;
                content = $@"
extern JSClassID get_js_{className}_class_id();
{getJSWrapperContent(className, "")}
if (JSWrapper{className}* wrapper{className} = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque2(ctx, val, get_js_{className}_class_id())))
{{
    instance->{field.LogicalOriginalName} = reinterpret_cast<decltype(instance->{field.LogicalOriginalName})>(wrapper{className}->instance);
    JS_SetPropertyStr(ctx, this_val, ""{magic}{field.LogicalOriginalName}"", val);
    JS_DupValue(ctx, val);
    return JS_UNDEFINED;
}}";
            }
            else
            {
                Dictionary<string, string> typeMap = getToTypeMap();
                Dictionary<string, string> typeDMap = getTypeDMap();

                string fieldType = getNamespaceFieldType(field);
                string jsFunc = "";
                string type = "";
                if (typeMap.ContainsKey(fieldType))
                {
                    jsFunc = typeMap[fieldType];
                }
                if (typeDMap.ContainsKey(fieldType))
                {
                    type = typeDMap[fieldType];
                }
                if (fieldType == "std::string")
                {
                    content = $@"
instance->{field.LogicalOriginalName} = JS_ToCString(ctx, val);
return JS_UNDEFINED;";
                }
                else
                {
                    content = $@"
{type} value;
verify({jsFunc}(ctx, &value, val) >= 0);
instance->{field.LogicalOriginalName} = static_cast<decltype(instance->{field.LogicalOriginalName})>(value);
return JS_UNDEFINED;";
                }
            }

            string elseif = magic == 0 ? "if" : "else if";
            string ret = $@"
{elseif} (magic == {magic})
{{
    {content}
}}";
            return ret;
        }

        public string getRegisterClassCallerExternContent()
        {
            string className = @class.Name;
            string ret = $@"
extern int js_{className}_SetModuleExport(JSContext* ctx, JSModuleDef* def);
extern int js_{className}_AddModuleExport(JSContext* ctx, JSModuleDef* def);";
            return ret;
        }

        public string[] getRegisterClassCallerContent()
        {
            string className = @class.Name;
            string[] array = { $@"js_{className}_SetModuleExport(ctx, def);", $@"js_{className}_AddModuleExport(ctx, def);" };
            return array;
        }

        private string getFullClassName()
        {
            string full = @class.Name;
            DeclarationContext declarationContext = @class.Namespace;
            while (declarationContext != null)
            {
                full = $"{declarationContext.Name}::" + full;
                declarationContext = declarationContext.Namespace;
            }
            return full;
        }

        List<Method> getSupportContructorMethod()
        {
            List<Method> methods = new List<Method>();
            foreach (Method constructor in @class.Constructors)
            {
                if (constructor.IsConstructor &&
                    constructor.IsDeleted == false &&
                    constructor.IsCopyConstructor == false &&
                    constructor.IsMoveConstructor == false)
                {
                    methods.Add(constructor);
                }
            }
            return methods;
        }

        private List<Method> getSupportMemberMethod()
        {
            List<Method> methods = new List<Method>();
            foreach (Method memberFunction in @class.Methods)
            {
                if (memberFunction.IsConstructor == false
                    && memberFunction.IsDestructor == false
                    && memberFunction.IsCopyConstructor == false
                    && memberFunction.Kind != CXXMethodKind.Operator)
                {
                    methods.Add(memberFunction);
                }
            }
            return methods;
        }

        private string getHeaderFileContent()
        {
            string className = @class.Name;
            string headerFilePath = @class.TranslationUnit.IncludePath;

            string ret = $@"
#pragma once
#include ""{headerFilePath}""
#include ""_QuickjsHelper.h""
{getJSWrapperContent(className, $@"
static inline JSWrapper{className}* UnretainedSetOpaque(JSValue objectClass)
{{
    JSWrapper{className} * wrapper = new JSWrapper{className}();
    JS_SetOpaque(objectClass, wrapper);
    return wrapper;
}}")}
JSClassID get_js_{className}_class_id();
";
            return ret;
        }

        private string getJSWrapperContent(string className, string content)
        {
            return $@"
struct JSWrapper{className}
{{
    EMemoryHostType HostType = EMemoryHostType::JS;
    EMemoryType MemoryType = EMemoryType::RawPtr;
	{className}* instance = nullptr;
    {content}
}};";
        }

        public void save()
        {
            System.IO.Directory.CreateDirectory(outputPath);

            string className = @class.Name;
            string fileName = $"Class{className}Register.cpp";
            string headerFileName = $"Class{className}Register.h";
            System.IO.File.WriteAllText(outputPath + "/" + fileName, getSourceFileContent());
            System.IO.File.WriteAllText(outputPath + "/" + headerFileName, getHeaderFileContent());
        }
    }
}
