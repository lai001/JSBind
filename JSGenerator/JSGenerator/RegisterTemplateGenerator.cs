using CppSharp.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace JSGenerator
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
           => self.Select((item, index) => (item, index));
    }

    class RegisterTemplateGenerator : IRegister
    {
        private readonly ASTContext ctx;
        private readonly Class @class;

        public RegisterTemplateGenerator(ASTContext ctx, Class @class)
        {
            this.@class = @class;
            this.ctx = ctx;
        }

        public string GetHeaderFileContent()
        {
            return $@"#ifndef Gen_{@class.Name}_H
#define Gen_{@class.Name}_H

#include <vector>
extern ""C""
{{
#include ""quickjs.h""
}}

namespace jsbind
{{
struct {@class.Name}
{{
    static JSClassID classID;
    static JSClassDef classDef;
    static std::vector<JSCFunctionListEntry> classProtoFuncs;
    static JSValue import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef);
}};
}}

#endif";
        }

        private string Ctor()
        {
            List<CppSharp.AST.Method> Methods = Misc.GetConstructorMethods(@class);
            string Impl = "";
            Methods.Sort(delegate (CppSharp.AST.Method x, CppSharp.AST.Method y)
            {
                return y.Parameters.Count.CompareTo(x.Parameters.Count);
            });
            foreach (var (Method, MethodIndex) in Methods.WithIndex())
            {
                string argv = "";
                foreach (var (Parameter, ParameterIndex) in Method.Parameters.WithIndex())
                {
                    if (Parameter.QualifiedType.Type is BuiltinType builtinType)
                    {
                        switch (builtinType.Type)
                        {
                            case PrimitiveType.Null:
                                break;
                            case PrimitiveType.Void:
                                break;
                            case PrimitiveType.Bool:
                                argv += $@"bool argv{ParameterIndex};
JS_ToBool(ctx, argv[{ParameterIndex}]);";
                                break;
                            case PrimitiveType.WideChar:
                            case PrimitiveType.Char:
                            case PrimitiveType.SChar:
                            case PrimitiveType.UChar:
                            case PrimitiveType.Char16:
                            case PrimitiveType.Char32:
                            case PrimitiveType.Short:
                            case PrimitiveType.UShort:
                            case PrimitiveType.Int:
                                argv += $@"int argv{ParameterIndex};
JS_ToInt32(ctx, &argv{ParameterIndex}, argv[{ParameterIndex}]);";
                                break;
                            case PrimitiveType.Int128:
                            case PrimitiveType.Long:
                            case PrimitiveType.LongLong:
                                argv += $@"long long argv{ParameterIndex};
JS_ToInt64(ctx, &argv{ParameterIndex}, argv[{ParameterIndex}]);";
                                break;
                            case PrimitiveType.UInt:
                            case PrimitiveType.UInt128:
                            case PrimitiveType.ULongLong:
                            case PrimitiveType.ULong:
                                argv += $@"unsigned int argv{ParameterIndex}; 
JS_ToUint32(ctx, &argv{ParameterIndex}, argv[{ParameterIndex}]);";
                                break;
                            case PrimitiveType.Half:
                            case PrimitiveType.Float:
                            case PrimitiveType.Double:
                            case PrimitiveType.Float128:
                            case PrimitiveType.Decimal:
                            case PrimitiveType.LongDouble:
                                argv += $@"double argv{ParameterIndex}; 
JS_ToFloat64(ctx, &argv{ParameterIndex}, argv[{ParameterIndex}]);";
                                break;
                            case PrimitiveType.IntPtr:
                                break;
                            case PrimitiveType.UIntPtr:
                                break;
                            case PrimitiveType.String:
                                argv += $@"char* argv{ParameterIndex};
argv{ParameterIndex} = JS_ToCString(ctx, argv[{ParameterIndex}]);";
                                break;
                        }
                    }
                }
                string argvList = "";
                for (int i = 0; i < Method.Parameters.Count; i++) {
                    argvList += $"argv{i},";
                }
                if (argvList.Length > 0)
                {
                    argvList = argvList.Remove(argvList.Length - 1);
                }
                Impl += $@"
{(MethodIndex > 0 ? "else" : "")} if (argc > {Method.Parameters.Count - 1})
{{
    {argv}
    ::{@class.Name} *nativeObject = new ::{@class.Name}({argvList});
    obj = JS_NewObjectClass(ctx, jsbind::{@class.Name}::classID);
    jsbind::NativeObject<::{@class.Name}>::setNativeObjectPointer(ctx, obj, nativeObject, true);
}}";
            }

            return $@"static JSValue ctor(JSContext *ctx, JSValueConst newTarget, int argc, JSValueConst *argv)
{{
    JSValue obj = JS_EXCEPTION;
{Impl}
    return obj;
}}";
        }

        private string MembersFunc()
        {
            string content = "";
            List<Method> Methods = Misc.GetSupportMemberMethods(@class);
            foreach (CppSharp.AST.Method Method in Methods)
            {
                int Count = 0;
                foreach (Parameter Parameter in Method.Parameters)
                {
                    ParameterKind Kind = Parameter.Kind;
                    if (Kind != ParameterKind.IndirectReturnType)
                    {
                        Count += 1;
                    }
                }
                string argc = "";
                if (Count > 0)
                {
                    argc = $@"if (argc > {Count})
{{
}}";
                }
                content += $@"static JSValue {Method.Name}(JSContext *ctx, JSValueConst thisVal, int argc, JSValueConst *argv)
{{
    JSValue ret = JS_EXCEPTION;
   {@class.QualifiedLogicalName} *nativeObject = jsbind::NativeObject<{@class.QualifiedLogicalName}>::getNativeObjectPointer(ctx, thisVal);
    if (nativeObject)
    {{
        {argc}
    }}
    return ret;
}}";
            }

            return content;
        }

        private string PropertyGetFunc()
        {
            string content = "";

            foreach (Field Field in @class.Fields)
            {
                string Impl = "";
                if (Field.QualifiedType.Type is TypedefType typedefType)
                {
                    if (typedefType.Declaration.Namespace.LogicalOriginalName == "std" && typedefType.Declaration.Name == "string")
                    {
                        Impl = $@"ret = JS_NewString(ctx, nativeObject->{Field.Name}.c_str());";
                    }
                }

                if (Field.QualifiedType.Type is PointerType pointerType)
                {
                    if (pointerType.Pointee is BuiltinType builtinType1)
                    {
                        if (builtinType1.Type == PrimitiveType.Char)
                        {
                            Impl = $@"ret = JS_NewString(ctx, nativeObject->{Field.Name});";
                        }
                    }
                }

                if (Field.QualifiedType.Type is BuiltinType builtinType)
                {
                    switch (builtinType.Type)
                    {
                        case PrimitiveType.Null:
                            break;
                        case PrimitiveType.Void:
                            break;
                        case PrimitiveType.Bool:
                            Impl = $@"ret = JS_NewBool(ctx, nativeObject->{Field.Name});";
                            break;
                        case PrimitiveType.WideChar:
                        case PrimitiveType.Char:
                        case PrimitiveType.SChar:
                        case PrimitiveType.UChar:
                        case PrimitiveType.Char16:
                        case PrimitiveType.Char32:
                        case PrimitiveType.Short:
                        case PrimitiveType.UShort:
                        case PrimitiveType.Int:
                            Impl = $@"ret = JS_NewInt32(ctx, nativeObject->{Field.Name});";
                            break;
                        case PrimitiveType.Int128:
                        case PrimitiveType.Long:
                        case PrimitiveType.LongLong:
                            Impl = $@"ret = JS_NewInt64(ctx, nativeObject->{Field.Name});";
                            break;
                        case PrimitiveType.UInt:
                        case PrimitiveType.UInt128:
                        case PrimitiveType.ULongLong:
                        case PrimitiveType.ULong:
                            Impl = $@"ret = JS_NewUint32(ctx, nativeObject->{Field.Name});";
                            break;
                        case PrimitiveType.Half:
                        case PrimitiveType.Float:
                        case PrimitiveType.Double:
                        case PrimitiveType.Float128:
                        case PrimitiveType.Decimal:
                        case PrimitiveType.LongDouble:
                            Impl = $@"ret = JS_NewFloat64(ctx, nativeObject->{Field.Name});";
                            break;
                        case PrimitiveType.IntPtr:
                            break;
                        case PrimitiveType.UIntPtr:
                            break;
                        case PrimitiveType.String:
                            Impl = $@"ret = JS_NewString(ctx, nativeObject->{Field.Name});";
                            break;
                    }
                }

                content += $@"static JSValue {Field.Name}(JSContext *ctx, JSValueConst thisVal)
{{
    JSValue ret = JS_EXCEPTION;
    {@class.QualifiedLogicalName} *nativeObject = jsbind::NativeObject<{@class.QualifiedLogicalName}>::getNativeObjectPointer(ctx, thisVal);
    if (nativeObject)
    {{
        {Impl}
    }}
    return ret;
}}";
            }

            return content;
        }

        private string PropertySetFunc()
        {
            string content = "";

            foreach (Field Field in @class.Fields)
            {
                string Impl = "";
                if (Field.QualifiedType.Type is TypedefType typedefType)
                {
                    if (typedefType.Declaration.Namespace.LogicalOriginalName == "std" && typedefType.Declaration.Name == "string")
                    {
                        Impl = $@"nativeObject->{Field.Name} = JS_ToCString(ctx, thisVal);";
                    }
                }

                if (Field.QualifiedType.Type is PointerType pointerType)
                {
                    if (pointerType.Pointee is TagType tagType)
                    {
                        Impl = $@"::{tagType.Declaration.QualifiedLogicalName} *nativeObject{tagType.Declaration.QualifiedLogicalName} = jsbind::NativeObject<::{tagType.Declaration.QualifiedLogicalName}>::getNativeObjectPointer(ctx, val);
        nativeObject->{Field.Name} = nativeObject{tagType.Declaration.QualifiedLogicalName};";
                    }
                }

                if (Field.QualifiedType.Type is BuiltinType builtinType)
                {
                    switch (builtinType.Type)
                    {
                        case PrimitiveType.Null:
                            break;
                        case PrimitiveType.Void:
                            break;
                        case PrimitiveType.Bool:
                            Impl = $@"nativeObject->{Field.Name} = JS_ToBool(ctx, thisVal);";
                            break;
                        case PrimitiveType.WideChar:
                        case PrimitiveType.Char:
                        case PrimitiveType.SChar:
                        case PrimitiveType.UChar:
                        case PrimitiveType.Char16:
                        case PrimitiveType.Char32:
                        case PrimitiveType.Short:
                        case PrimitiveType.UShort:
                        case PrimitiveType.Int:
                            Impl = $@"int value;
JS_ToInt32(ctx, &value, thisVal);
nativeObject->{Field.Name} = value;";
                            break;
                        case PrimitiveType.Int128:
                        case PrimitiveType.Long:
                        case PrimitiveType.LongLong:
                            Impl = $@"long long value;
JS_ToInt64(ctx, &value, thisVal);
nativeObject->{Field.Name} = value;";
                            break;
                        case PrimitiveType.UInt:
                        case PrimitiveType.UInt128:
                        case PrimitiveType.ULongLong:
                        case PrimitiveType.ULong:
                            Impl = $@"unsigned int value; 
JS_ToUint32(ctx, &value, thisVal);
nativeObject->{Field.Name} = value;";
                            break;
                        case PrimitiveType.Half:
                        case PrimitiveType.Float:
                        case PrimitiveType.Double:
                        case PrimitiveType.Float128:
                        case PrimitiveType.Decimal:
                        case PrimitiveType.LongDouble:
                            Impl = $@"double value; 
JS_ToFloat64(ctx, &value, thisVal);
nativeObject->{Field.Name} = value;";
                            break;
                        case PrimitiveType.IntPtr:
                            break;
                        case PrimitiveType.UIntPtr:
                            break;
                        case PrimitiveType.String:
                            Impl = $@"nativeObject->{Field.Name} = JS_ToCString(ctx, thisVal);";
                            break;
                    }
                }

                content += $@"static JSValue {Field.Name}(JSContext *ctx, JSValueConst thisVal, JSValue val)
{{
    JSValue ret = JS_EXCEPTION;
    {@class.QualifiedLogicalName} *nativeObject = jsbind::NativeObject<{@class.QualifiedLogicalName}>::getNativeObjectPointer(ctx, thisVal);
    if (nativeObject)
    {{
            {Impl}
            return JS_UNDEFINED;
    }}
    return ret;
}}";
            }

            return content;
        }

        private string StaticInit()
        {
            return $@"JSClassID {@class.Name}::classID;
JSClassDef {@class.Name}::classDef;
std::vector<JSCFunctionListEntry> {@class.Name}::classProtoFuncs;";
        }

        private string Import()
        {
            string Push = "";
            List<Method> Methods = Misc.GetSupportMemberMethods(@class);
            foreach (Field Field in @class.Fields)
            {
                Push += $@"
classProtoFuncs.push_back(createMemberProperty(""{Field.Name}"", JSPropertyGet_{@class.Name}::{Field.Name}, JSPropertySet_{@class.Name}::{Field.Name}));";
            }
            foreach (Method Method in Methods)
            {
                Push += $@"
classProtoFuncs.push_back(createMemberFunc(""{Method.Name}"", JSFunc_{@class.Name}::{Method.Name}));";
            }

            return $@"JSValue {@class.Name}::import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef)
{{
    {@class.Name}::classDef.class_name = ""{@class.Name}"";
    {Push}
    JSValue constructor =
        importClass(ctx, {@class.Name}::classID, {@class.Name}::classDef, classProtoFuncs.data(), classProtoFuncs.size(), JSFunc_{@class.Name}::ctor);
    JS_SetPropertyStr(ctx, obj, {@class.Name}::classDef.class_name, constructor);
    NativeObject<::{@class.QualifiedLogicalName}>::className = ""@Native{@class.Name}"";
    NativeObject<::{@class.QualifiedLogicalName}>::import(ctx, obj, nullptr);
    return constructor;
}}";
        }

        public string GetSourceFileContent()
        {
            string nativeClassHeaderFilePath = @class.TranslationUnit.IncludePath;

            return $@"#include ""{@class.Name}.h""
#include ""{nativeClassHeaderFilePath}""
#include ""NativeObject.h""

namespace JSFunc_{@class.Name}
{{
{Ctor()}
{MembersFunc()}
}}

namespace JSPropertyGet_{@class.Name}
{{
{PropertyGetFunc()}
}} 

namespace JSPropertySet_{@class.Name}
{{
{PropertySetFunc()}
}} 

namespace jsbind
{{
{StaticInit()}
{Import()}
}}";
        }

        public void Save(string outputFolderPath)
        {
            string className = @class.Name;
            string fileName = $"{className}.cpp";
            string headerFileName = $"{className}.h";
            System.IO.Directory.CreateDirectory(outputFolderPath);
            System.IO.File.WriteAllText(outputFolderPath + "/" + headerFileName, GetHeaderFileContent());
            System.IO.File.WriteAllText(outputFolderPath + "/" + fileName, GetSourceFileContent());
        }

        public Tuple<string, string, string> GetRegisterClassCallerContent()
        {
            throw new NotImplementedException();
        }
    }

}
