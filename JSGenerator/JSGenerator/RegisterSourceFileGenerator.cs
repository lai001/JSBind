using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{
    class RegisterSourceFileGenerator : IRegister
    {
        private readonly ASTContext ctx;
        private readonly Class @class;

        public RegisterSourceFileGenerator(ASTContext ctx, Class @class)
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
JSWrapper{className}* wrapper = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque2(ctx, this_val, JS_GetClassID(this_val, nullptr)));
{className}* instance = wrapper->instance;";
            };
            string ret = @$"
{GetIncludeContent()}
static JSClassID js_{className}_class_id;

JSClassID get_js_{className}_class_id()
{{
    return js_{className}_class_id;
}}

{GetFinalizerContent()}
{GetCtorContent()}
{MemberFunctionGenerator.Get(@class, GetSupportMemberMethod(@class), retrieveInstance, null)}
{GetGetPropContent()}
{GetSetPropContent()}
{GetClassDefContent()}
{GetClassSetModuleExportContent()}
{GetClassAddModuleExportContent()}
";
            return ret;
        }

        public static string GetVectorIncludeContent(Class @class)
        {
            string vectorInclude = "";

            foreach (TemplateSpecializationType templateSpecializationType in FindVectorType.Instance.FindTemplateSpecializationTypes(@class).Values)
            {
                vectorInclude += $@"#include ""{CPPVectorRegisterGenerator.GetIncludeFileName(templateSpecializationType)}""";
                vectorInclude += "\n";
            }
            return vectorInclude;
        }

        public string GetIncludeContent()
        {
            string className = @class.Name;
            string headerFilePath = @class.TranslationUnit.IncludePath;
            string vectorInclude = GetVectorIncludeContent(@class);

            string ret = @$"
#include ""Class{className}Register.h""
{vectorInclude}
";
            return ret;
        }

        public static string GetVectorVarContent(Class @class)
        {
            string vectorVarContent = "";
            List<Field> fields = GetSupportFields(@class);
            foreach (Field field in fields)
            {
                if (field.Type is TemplateSpecializationType && FindVectorType.Instance.IsStdVector(field.Type as TemplateSpecializationType))
                {
                    TemplateSpecializationType templateSpecializationType = field.Type as TemplateSpecializationType;
                    string typeName = templateSpecializationType.Arguments[0].ToString();

                    vectorVarContent += $@"
{{
    JSValue cppvectorJSValue = JS_NewObjectClass(ctx, get_js_CPPVector_{typeName}_class_id());
    JSWrapperCPPVector_{typeName}* wrapper = JSWrapperCPPVector_{typeName}::UnretainedSetOpaque(cppvectorJSValue);
    wrapper->instance = new CPPVector_{typeName}();
    wrapper->instance->getVector = [instance]()
    {{
        return &(instance->cppvector);
    }};
    JS_SetPropertyStr(ctx, classObject, ""@{field.LogicalOriginalName}"", cppvectorJSValue);
}}
";
                }
            }
            return vectorVarContent;
        }

        public string GetCtorContent()
        {
            string className = @class.Name;
            string newContent = "";
            List<Method> methods = GetSupportContructorMethod(@class);
            string vectorVarContent = GetVectorVarContent(@class);

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
    instance = new {className}({vlist});
}}";
                }
                else
                {
                    newContent += $@"
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
    {newContent}
	JSWrapper{className}* wrapper = new JSWrapper{className}();
	wrapper->instance = instance;
    assert(instance);
	JSValue classObject = NewObjectProtoClass(ctx, js_{className}_class_id, new_target, wrapper);
    {vectorVarContent}
    return classObject;
}}";
            return ret;
        }

        public string GetFinalizerContent()
        {
            string className = @class.Name;
            string ret = $@"
static void js_{className}_finalizer(JSRuntime* rt, JSValue val)
{{
	if (JSWrapper{className}* wrapper = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque(val, JS_GetClassID(val, nullptr))))
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

        public string GetClassDefContent()
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

        public string GetClassAddModuleExportContent()
        {
            string className = @class.Name;
            string ret = $@"
int js_{className}_AddModuleExport(JSContext* ctx, JSModuleDef* def)
{{
	return JS_AddModuleExport(ctx, def, js_{className}_class()->class_name);
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
                propFunc += $@"js_cgetset_magic_def(""{name}"", js_{className}_get_prop, js_{className}_set_prop, {i})," + "\n";
            }

            List<Method> supportMemberMethods = GetSupportMemberMethod(@class);
            for (int i = 0; i < supportMemberMethods.Count; i++)
            {
                Method method = supportMemberMethods[i];
                string name = method.LogicalOriginalName;
                memberFunc += $@"js_cfunc_def(""{name}"", 0, JS{className}MemberFunction::{name})," + "\n";
            }
            string content = "";
            string baseClassContent = "";
            string baseClassName = null;

            if (@class.HasBaseClass)
            {
                baseClassName = @class.BaseClass.LogicalOriginalName;
            }
            else if (@class.Bases.Count > 0)
            {
                BaseClassSpecifier baseClassSpecifier = @class.Bases[0];
                if (baseClassSpecifier.Type is not TemplateSpecializationType)
                {
                    baseClassName = @class.Bases[0].Class.Name;
                }
            }
            if (baseClassName != null)
            {
                baseClassContent = $@"
extern JSClassID get_js_{baseClassName}_class_id();
JSValue prototype = JS_GetClassProto(ctx, get_js_{baseClassName}_class_id());
JS_SetConstructor(ctx, object, prototype);
JS_SetPrototype(ctx, object, prototype);
JS_FreeValue(ctx, prototype);
";
            }

            if (supportMemberMethods.Count + @class.Fields.Count > 0)
            {
                content = $@"
static JSCFunctionListEntry js_class_proto_funcs[] = {{
    {propFunc}
    {memberFunc}
}};
JSValue object;
SetModuleExportHelper(ctx, def, js_{className}_class(), &js_{className}_class_id, js_{className}_ctor, 2, js_class_proto_funcs, sizeof(js_class_proto_funcs) / sizeof((js_class_proto_funcs)[0]), &object);
{baseClassContent}
";
            }
            else
            {
                content = $@"
JSValue object;
SetModuleExportHelper(ctx, def, js_{className}_class(), js_{className}_class_id, js_{className}_ctor, 2, nullptr, 0, &object);
{baseClassContent}
";
            }

            string ret = $@"
int js_{className}_SetModuleExport(JSContext* ctx, JSModuleDef* def)
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
static JSValue js_{className}_get_prop(JSContext* ctx, JSValueConst this_val, int magic)
{{
	JSWrapper{className}* wrapper = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque2(ctx, this_val, JS_GetClassID(this_val, nullptr)));
	{className}* instance = wrapper->instance;
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

        public static string GetGetPropMagicContent(Field field, int magic)
        {
            string content = "";
            if (field.Type is PointerType)
            {
                content = $@"return JS_GetPropertyStr(ctx, this_val, ""{magic}{field.LogicalOriginalName}"");";
            }
            else if (field.Type is TemplateSpecializationType && FindVectorType.Instance.IsStdVector(field.Type as TemplateSpecializationType))
            {

                content = $@"return JS_GetPropertyStr(ctx, this_val, ""@{field.LogicalOriginalName}"");";
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
static JSValue js_{className}_set_prop(JSContext* ctx, JSValueConst this_val, JSValue val, int magic)
{{
	JSWrapper{className}* wrapper = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque2(ctx, this_val, JS_GetClassID(this_val, nullptr)));
	{className}* instance = wrapper->instance;
    {content}
	return JS_EXCEPTION;
}}";

            return ret;
        }

        public static string GetSetPropMagicContent(Field field, int magic)
        {
            string content = "";

            if (field.Type is PointerType)
            {
                PointerType pointerType = field.Type as PointerType;
                string className = (pointerType.Pointee as TagType).Declaration.Name;
                content = $@"
extern JSClassID get_js_{className}_class_id();
{GetJSWrapperContent(className, "")}
if (JSWrapper{className}* wrapper{className} = reinterpret_cast<JSWrapper{className}*>(JS_GetOpaque2(ctx, val, get_js_{className}_class_id())))
{{
    instance->{field.LogicalOriginalName} = reinterpret_cast<decltype(instance->{field.LogicalOriginalName})>(wrapper{className}->instance);
    JS_SetPropertyStr(ctx, this_val, ""{magic}{field.LogicalOriginalName}"", val);
    JS_DupValue(ctx, val);
    return JS_UNDEFINED;
}}";
            }
            else if (field.Type is TemplateSpecializationType && FindVectorType.Instance.IsStdVector(field.Type as TemplateSpecializationType))
            {
                TemplateSpecializationType templateSpecializationType = field.Type as TemplateSpecializationType;
                string typeName = templateSpecializationType.Arguments[0].ToString();
                Dictionary<string, string> typeMap = GetToTypeMap();
                string jsFunc = "";
                if (typeMap.ContainsKey(typeName))
                {
                    jsFunc = typeMap[typeName];
                }
                content += $@"
JSValue *arrpp;
uint32_t countp;
verify(JS_GetFastArray(ctx, val, &arrpp, &countp) >= 0);
instance->cppvector.clear();

for (uint32_t i = 0; i < countp; i++)
{{
    {typeName} value;
    verify({jsFunc}(ctx, &value, arrpp[i]) >= 0);
    instance->cppvector.push_back(value);
}}
return JS_UNDEFINED;
";
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

        public static List<Method> GetSupportContructorMethod(Class @class)
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

        public static List<Method> GetSupportMemberMethod(Class @class)
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

        public static List<Field> GetSupportFields(Class @class)
        {
            return @class.Fields;
        }

        private string GetHeaderFileContent()
        {
            string className = @class.Name;
            string headerFilePath = @class.TranslationUnit.IncludePath;

            string ret = $@"
#pragma once
#include ""{headerFilePath}""
#include ""_QuickjsHelper.h""
{GetJSWrapperContent(className, $@"
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

        public static string GetJSWrapperContent(string className, string content)
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

        public void Save(string outputFolderPath)
        {
            System.IO.Directory.CreateDirectory(outputFolderPath);

            string className = @class.Name;
            string fileName = $"Class{className}Register.cpp";
            string headerFileName = $"Class{className}Register.h";
            System.IO.File.WriteAllText(outputFolderPath + "/" + fileName, GetSourceFileContent());
            System.IO.File.WriteAllText(outputFolderPath + "/" + headerFileName, GetHeaderFileContent());
        }

        public Tuple<string, string, string> GetRegisterClassCallerContent()
        {
            string className = @class.Name;
            Tuple<string, string, string> tuple = new Tuple<string, string, string>($@"
extern int js_{className}_SetModuleExport(JSContext* ctx, JSModuleDef* def);
extern int js_{className}_AddModuleExport(JSContext* ctx, JSModuleDef* def);",
$@"js_{className}_SetModuleExport(ctx, def);",
$@"js_{className}_AddModuleExport(ctx, def);");
            return tuple;
        }
    }
}
