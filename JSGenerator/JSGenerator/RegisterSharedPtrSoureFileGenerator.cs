using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{
    class RegisterSharedPtrSoureFileGenerator : IRegister
    {
        private readonly ASTContext ctx;
        private readonly Class @class;

        public RegisterSharedPtrSoureFileGenerator(ASTContext ctx, Class @class)
        {
            this.@class = @class;
            this.ctx = ctx;
        }

        public string GetSourceFileContent()
        {
            string className = @class.Name;
            Func<string> retrieveInstance = delegate
            {
                return $@"
JSWrapperSharedPtr{className}* wrapper = reinterpret_cast<JSWrapperSharedPtr{className}*>(JS_GetOpaque2(ctx, this_val, JS_GetClassID(this_val, nullptr)));
std::shared_ptr<{className}> instance = wrapper->instance;";
            };
            Func<string> getRawPtrFunc = delegate
            {
                return $@"
static JSValue _get(JSContext *ctx, JSValueConst this_val, int argc, JSValueConst *argv)
{{
    JSValue rawPtrValue = JS_GetPropertyStr(ctx, this_val, ""@rawPtr"");
    assert(JS_IsException(rawPtrValue) == false);
    return rawPtrValue;
}}";
            };
            string ret = @$"
{GetIncludeContent()}
static JSClassID js_SharedPtr{className}_class_id;

JSClassID get_js_SharedPtr{className}_class_id()
{{
    return js_SharedPtr{className}_class_id;
}}

{GetFinalizerContent()}
{GetCtorContent()}
{MemberFunctionGenerator.Get(@class, RegisterSourceFileGenerator.GetSupportMemberMethod(@class), retrieveInstance, getRawPtrFunc)}
{GetGetPropContent()}
{GetSetPropContent()}
{GetClassDefContent()}
{GetClassSetModuleExportContent()}
{GetClassAddModuleExportContent()}
";
            return ret;
        }

        public string GetIncludeContent()
        {
            string className = @class.Name;
            string headerFilePath = @class.TranslationUnit.IncludePath;
            string vectorInclude = RegisterSourceFileGenerator.GetVectorIncludeContent(@class);

            string ret = @$"
#include ""SharedPtrClass{className}Register.h""
#include ""Class{className}Register.h""
{vectorInclude}
";
            return ret;
        }

        public string GetCtorContent()
        {
            string className = @class.Name;
            string newContent = "";
            
            string vectorVarContent = RegisterSourceFileGenerator.GetVectorVarContent(@class);

            List<Method> methods = RegisterSourceFileGenerator.GetSupportContructorMethod(@class);

            for (int i = 0; i < methods.Count; i++)
            {
                Method method = methods[i];
                if (method.Parameters.Count > 0)
                {
                    string parametersCodeLine = "";
                    for (int parameterIndex = 0; parameterIndex < method.Parameters.Count; parameterIndex++)
                    {
                        Parameter parameter = method.Parameters[parameterIndex];
                        parametersCodeLine += MemberFunctionGenerator.GetParameterContent(parameter, parameterIndex);
                    }

                    string vlist = MemberFunctionGenerator.GetVlist(method.Parameters.Count);
                    newContent += $@"
if (argc == {method.Parameters.Count})
{{
    {parametersCodeLine}
    instance = std::make_shared<{className}>({vlist});
}}";
                }
                else
                {
                    newContent += $@"
if (argc == {method.Parameters.Count})
{{
    instance = std::make_shared<{className}>();
}}";
                }
            }

            string ret = @$"
static JSValue js_SharedPtr{className}_ctor(JSContext* ctx, JSValueConst new_target, int argc, JSValueConst* argv)
{{
    std::shared_ptr<{className}> instance = nullptr;
    {newContent}
	JSWrapperSharedPtr{className}* wrapper = new JSWrapperSharedPtr{className}();
	wrapper->instance = instance;
    assert(instance);
	JSValue classObject = NewObjectProtoClass(ctx, js_SharedPtr{className}_class_id, new_target, wrapper);
    JSWrapper{className}* wrapper{className} = new JSWrapper{className}();
    wrapper{className}->instance = instance.get();
    wrapper{className}->HostType = EMemoryHostType::Cpp;
    JSValue object = JS_NewObjectClass(ctx, get_js_{className}_class_id());
    assert(JS_IsException(object) == false);
    JS_SetOpaque(object, wrapper{className});
    JS_SetPropertyStr(ctx, classObject, ""@rawPtr"", object);
    {vectorVarContent}
    return classObject;
}}";
            return ret;
        }

        public string GetFinalizerContent()
        {
            string className = @class.Name;
            string ret = $@"
static void js_SharedPtr{className}_finalizer(JSRuntime* rt, JSValue val)
{{
	if (JSWrapperSharedPtr{className}* wrapper = reinterpret_cast<JSWrapperSharedPtr{className}*>(JS_GetOpaque(val, js_SharedPtr{className}_class_id)))
	{{
        wrapper->instance = nullptr;
		delete wrapper;
	}}
}}";
            return ret;
        }

        public string GetClassDefContent()
        {
            string className = @class.Name;
            string ret = $@"
static JSClassDef* js_SharedPtr{className}_class()
{{
	static JSClassDef class_def;
	class_def.class_name = ""SharedPtr{className}"";
	class_def.finalizer = js_SharedPtr{className}_finalizer;
	return &class_def;
}}";
            return ret;
        }

        public string GetClassAddModuleExportContent()
        {
            string className = @class.Name;
            string ret = $@"
int js_SharedPtr{className}_AddModuleExport(JSContext* ctx, JSModuleDef* def)
{{
	return JS_AddModuleExport(ctx, def, js_SharedPtr{className}_class()->class_name);
}}";
            return ret;
        }

        public string GetClassSetModuleExportContent()
        {
            string className = @class.Name;
            string propFunc = "";
            string memberFunc = "";
            for (int i = 0; i < @class.Fields.Count; i++)
            {
                Field field = @class.Fields[i];
                string name = field.LogicalOriginalName;
                propFunc += $@"js_cgetset_magic_def(""{name}"", js_SharedPtr{className}_get_prop, js_SharedPtr{className}_set_prop, {i})," + "\n";
            }

            List<Method> supportMemberMethods = RegisterSourceFileGenerator.GetSupportMemberMethod(@class);
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
    js_cfunc_def(""_get"", 0, JS{className}MemberFunction::_get),
}};
SetModuleExportHelper(ctx, def, js_SharedPtr{className}_class(), &js_SharedPtr{className}_class_id, js_SharedPtr{className}_ctor, 2, js_class_proto_funcs, sizeof(js_class_proto_funcs) / sizeof((js_class_proto_funcs)[0]), nullptr);
";
            }
            else
            {
                content = $@"SetModuleExportHelper(ctx, def, js_SharedPtr{className}_class(), js_SharedPtr{className}_class_id, js_SharedPtr{className}_ctor, 2, nullptr, 0, nullptr);";
            }

            string ret = $@"
int js_SharedPtr{className}_SetModuleExport(JSContext* ctx, JSModuleDef* def)
{{
	{content}
	return 0;
}}";
            return ret;
        }

        public string GetGetPropContent()
        {
            string className = @class.Name;
            string content = "";
            for (int i = 0; i < @class.Fields.Count; i++)
            {
                Field field = @class.Fields[i];
                content += GetGetPropMagicContent(field, i) + "\n";
            }

            string ret = $@"
static JSValue js_SharedPtr{className}_get_prop(JSContext* ctx, JSValueConst this_val, int magic)
{{
	JSWrapperSharedPtr{className}* wrapper = reinterpret_cast< JSWrapperSharedPtr{className}*>(JS_GetOpaque2(ctx, this_val, JS_GetClassID(this_val, nullptr)));
	std::shared_ptr<{className}> instance = wrapper->instance;
    {content}
	return JS_EXCEPTION;
}}";

            return ret;
        }

        private static Dictionary<string, string> GetNewTypeMap()
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

        private static Dictionary<string, string> GetToTypeMap()
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

        private static Dictionary<string, string> GetTypeDMap()
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

        private static string GetNamespaceFieldType(Field field)
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

        public string GetGetPropMagicContent(Field field, int magic)
        {
            return RegisterSourceFileGenerator.GetGetPropMagicContent(field, magic);
            string content = "";
            if (field.Type is PointerType)
            {
                content = $@"return JS_GetPropertyStr(ctx, this_val, ""{magic}{field.LogicalOriginalName}"");";
            }
            else
            {
                Dictionary<string, string> typeMap = GetNewTypeMap();
                string fieldType = GetNamespaceFieldType(field);
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

        public string GetSetPropContent()
        {
            string className = @class.Name;
            string content = "";
            for (int i = 0; i < @class.Fields.Count; i++)
            {
                Field field = @class.Fields[i];
                content += GetSetPropMagicContent(field, i) + "\n";
            }

            string ret = $@"
static JSValue js_SharedPtr{className}_set_prop(JSContext* ctx, JSValueConst this_val, JSValue val, int magic)
{{
	JSWrapperSharedPtr{className}* wrapper = reinterpret_cast<JSWrapperSharedPtr{className}*>(JS_GetOpaque2(ctx, this_val, JS_GetClassID(this_val, nullptr)));
    std::shared_ptr<{className}> instance = wrapper->instance;
    {content}
	return JS_EXCEPTION;
}}";

            return ret;
        }

        public string GetSetPropMagicContent(Field field, int magic)
        {
            return RegisterSourceFileGenerator.GetSetPropMagicContent(field, magic);
            string content = "";

            if (field.Type is PointerType)
            {
                PointerType pointerType = field.Type as PointerType;
                string className = (pointerType.Pointee as TagType).Declaration.Name;
                content = $@"
extern JSClassID get_js_{className}_class_id();
struct JSWrapper{className}
{{
    EMemoryHostType HostType = EMemoryHostType::JS;
    EMemoryType MemoryType = EMemoryType::RawPtr;
    {className} *instance = nullptr;
}};
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
                Dictionary<string, string> typeMap = GetToTypeMap();
                Dictionary<string, string> typeDMap = GetTypeDMap();

                string fieldType = GetNamespaceFieldType(field);
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

//        public string getRegisterClassCallerExternContent()
//        {
//            string className = @class.Name;
//            string ret = $@"
//extern int js_SharedPtr{className}_SetModuleExport(JSContext* ctx, JSModuleDef* def);
//extern int js_SharedPtr{className}_AddModuleExport(JSContext* ctx, JSModuleDef* def);";
//            return ret;
//        }

//        public string[] getRegisterClassCallerContent()
//        {
//            string className = @class.Name;
//            string[] array = { $@"js_SharedPtr{className}_SetModuleExport(ctx, def);", $@"js_SharedPtr{className}_AddModuleExport(ctx, def);" };
//            return array;
//        }

        private string GetFullClassName()
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

        //List<Method> getSupportContructorMethod()
        //{
        //    List<Method> methods = new List<Method>();
        //    foreach (Method constructor in @class.Constructors)
        //    {
        //        if (constructor.IsConstructor &&
        //            constructor.IsDeleted == false &&
        //            constructor.IsCopyConstructor == false &&
        //            constructor.IsMoveConstructor == false)
        //        {
        //            methods.Add(constructor);
        //        }
        //    }
        //    return methods;
        //}

        //private List<Method> getSupportMemberMethod()
        //{
        //    List<Method> methods = new List<Method>();
        //    foreach (Method memberFunction in @class.Methods)
        //    {
        //        if (memberFunction.IsConstructor == false
        //            && memberFunction.IsDestructor == false
        //            && memberFunction.IsCopyConstructor == false
        //            && memberFunction.Kind != CXXMethodKind.Operator)
        //        {
        //            methods.Add(memberFunction);
        //        }
        //    }
        //    return methods;
        //}

        private string GetHeaderFileContent()
        {
            string className = @class.Name;
            string headerFilePath = @class.TranslationUnit.IncludePath;

            string ret = $@"
#pragma once
#include <memory>
#include ""{headerFilePath}""
#include ""_QuickjsHelper.h""
{GetJSWrapperContent(className, $@"
static inline JSWrapperSharedPtr{className}* UnretainedSetOpaque(JSValue objectClass)
{{
    JSWrapperSharedPtr{className} * wrapper = new JSWrapperSharedPtr{className}();
    JS_SetOpaque(objectClass, wrapper);
    return wrapper;
}}")}
JSClassID get_js_SharedPtr{className}_class_id();
";
            return ret;
        }

        private string GetJSWrapperContent(string className, string content)
        {
            return $@"
struct JSWrapperSharedPtr{className}
{{
	std::shared_ptr<{className}> instance = nullptr;
    {content}
}};";
        }

        public void Save(string outputFolderPath)
        {
            System.IO.Directory.CreateDirectory(outputFolderPath);

            string className = @class.Name;
            string fileName = $"SharedPtrClass{className}Register.cpp";
            string headerFileName = $"SharedPtrClass{className}Register.h";
            System.IO.File.WriteAllText(outputFolderPath + "/" + fileName, GetSourceFileContent());
            System.IO.File.WriteAllText(outputFolderPath + "/" + headerFileName, GetHeaderFileContent());
        }

        Tuple<string, string, string> IRegister.GetRegisterClassCallerContent()
        {
            string className = @class.Name;
            Tuple<string, string, string> tuple = new Tuple<string, string, string>($@"
extern int js_SharedPtr{className}_SetModuleExport(JSContext* ctx, JSModuleDef* def);
extern int js_SharedPtr{className}_AddModuleExport(JSContext* ctx, JSModuleDef* def);",
$@"js_SharedPtr{className}_SetModuleExport(ctx, def);",
$@"js_SharedPtr{className}_AddModuleExport(ctx, def);");
            return tuple;
        }
    }
}
