using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{
	class Misc
	{
        public static List<Method> GetSupportMemberMethods(Class @class)
        {
            List<Method> methods = new();
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

        public static List<Method> GetConstructorMethods(Class @class)
        {
            List<Method> methods = new();
            foreach (Method memberFunction in @class.Methods)
            {
                if (memberFunction.IsConstructor == true && memberFunction.Access == AccessSpecifier.Public && memberFunction.IsCopyConstructor == false)
                {
                    methods.Add(memberFunction);
                }
            }
            return methods;
        }

        public static List<Class> FindClasses(ASTContext ctx, List<string> classNames)
        {
            List<Class> classes = new();
            foreach (string className in classNames)
            {
                classes.AddRange(FindClassE(ctx, className));
            }
            return classes;
        }

        private static List<Class> FindClassE(ASTContext ctx, string name)
        {
            List<Class> classes = new();
            Action<DeclarationContext> walk = delegate (DeclarationContext declarationContext) { };

            walk = delegate (DeclarationContext declarationContext)
            {
                // Console.WriteLine(declarationContext.Name);

                Class @class = declarationContext.FindClass(name);
                if (@class != null)
                {
                    classes.Add(@class);
                }
                foreach (var item in declarationContext.Namespaces)
                {
                    walk(item);
                }
            };

            foreach (var item in ctx.TranslationUnits)
            {
                if (item.IsSystemHeader == false)
                {
                    walk(item);
                }
            }

            return classes;
        }

    }

}