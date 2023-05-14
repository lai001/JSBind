using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{
    class MemberFunctionGenerator
    {
        private class ReturnTypeInfo
        {
            public readonly string returnType = "";
            public readonly string namespaceReturnType = "";
            public readonly string @namespace = "";
            public readonly Parameter returnParameter = null;
            public ReturnTypeInfo(Method method)
            {
                if (method.OriginalReturnType.Type is TypedefType)
                {
                    TypedefType typedefType = method.OriginalReturnType.Type as TypedefType;
                    @namespace = typedefType.Declaration.OriginalNamespace.ToString();
                    returnType = method.OriginalReturnType.ToString();
                }
                else
                {
                    returnType = method.ReturnType.ToString();
                }

                if (@namespace.Length > 0)
                {
                    namespaceReturnType = @namespace + "::" + returnType;
                }
                else
                {
                    namespaceReturnType = returnType;
                }

                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    Parameter parameter = method.Parameters[i];
                    if (parameter.Kind == ParameterKind.IndirectReturnType)
                    {
                        returnParameter = parameter;
                    }
                }
            }
        }

        public static string Get(Class @class, List<Method> methods, Func<string> retrieveInstance, Func<string> addtional)
        {
            string className = @class.Name;
            string content = "";
            string add = "";
            foreach (Method method in methods)
            {
                content += GetMemberFunctionContent(@class, method, retrieveInstance);
            }
            if (addtional != null)
            {
                add = addtional();
            }
            string ret = $@"
struct JS{className}MemberFunction
{{
{content}
{add}
}};";
            return ret;
        }

        private static string GetMemberFunctionContent(Class @class, Method method, Func<string> retrieveInstance)
        {
            string className = @class.Name;
            string methodName = method.LogicalOriginalName;
            string parametersCodeLine = "";
            ReturnTypeInfo returnTypeInfo = new ReturnTypeInfo(method);
            List<Parameter> parameters = new List<Parameter>();
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                Parameter parameter = method.Parameters[i];
                if (parameter.Kind != ParameterKind.IndirectReturnType)
                {
                    parameters.Add(parameter);
                }
            }
            string vlist = GetVlist(parameters.Count);

            for (int i = 0; i < parameters.Count; i++)
            {
                Parameter parameter = parameters[i];
                parametersCodeLine += GetParameterContent(parameter, i);
            }

            string ret = $@"
static JSValue {methodName}(JSContext* ctx, JSValueConst this_val, int argc, JSValueConst* argv)
{{
    {retrieveInstance()}
	assert(instance);
    assert(argc == {parameters.Count});
    {parametersCodeLine}
    {GetMethodCallContent(method, returnTypeInfo, vlist)}
	{GetReturnCodeContent(returnTypeInfo)}
}}
";
            return ret;
        }

        public static string GetVlist(int parametersCount, int offset = 0)
        {
            string vlist = "";
            for (int i = 0; i < parametersCount; i++)
            {
                vlist += $@"v{i + offset},";
            }
            if (vlist.Length > 0)
            {
                vlist = vlist.Remove(vlist.LastIndexOf(","));
            }
            return vlist;
        }

        private static string GetReturnCodeContent(ReturnTypeInfo returnTypeInfo)
        {
            if (returnTypeInfo.namespaceReturnType == "std::string")
            {
                return $@"return JS_NewString(ctx, r.c_str());";
            }
            else if (returnTypeInfo.namespaceReturnType == "int")
            {
                return $@"return JS_NewInt32(ctx, r);";
            }
            else if (returnTypeInfo.namespaceReturnType == "float" || returnTypeInfo.namespaceReturnType == "double")
            {
                return $@"return JS_NewFloat64(ctx, r);";
            }
            else if (returnTypeInfo.namespaceReturnType == "void")
            {
                return "return JS_UNDEFINED;";
            }

            return "return JS_EXCEPTION;";
        }

        private static string GetMethodCallContent(Method method, ReturnTypeInfo returnTypeInfo, string vlist)
        {
            string methodName = method.LogicalOriginalName;
            string ret = "";
            if (returnTypeInfo.returnType == "void")
            {
                ret = $@"instance->{methodName}({vlist});";
            }
            else
            {
                ret = $@"{returnTypeInfo.namespaceReturnType} r = instance->{methodName}({vlist});";
            }
            return ret;
        }

        public static string GetParameterContent(Parameter parameter, int index)
        {
            string type = parameter.Type.ToString();
            string c = "";

            if (type == "int")
            {
                c = $@"verify(JS_ToInt32(ctx, &v{index}, argv[{index}]) >= 0);";

            }
            else if (type == "double" || type == "float")
            {
                c = $@"verify(JS_ToFloat64(ctx, &v{index}, argv[{index}]) >= 0);";
            }
            else if (type == "ulong")
            {
                type = "int64_t";
                c = $@"verify(JS_ToInt64(ctx, &v{index}, argv[{index}]) >= 0);";
            }

            string ret = $@"
{type} v{index};
{c}
";
            return ret;
        }
    }
}
