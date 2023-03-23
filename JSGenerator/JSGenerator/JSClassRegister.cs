using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{
    class JSClassRegister
    {
        private ASTContext ctx;

        public JSClassRegister(ASTContext ctx, List<string> classNames)
        {
            string ouputPath = "../Register";
            new HelperGenerator(ouputPath).save();
            this.ctx = ctx;
            List<Class> classes = findClasses(classNames);
            List<RegisterSourceFileGenerator> registerSourceFileGenerators = new List<RegisterSourceFileGenerator>();
            List<RegisterSharedPtrSoureFileGenerator> registerSharedPtrSoureFileGenerators = new List<RegisterSharedPtrSoureFileGenerator>();
            RegisterAllClassFileGenerator registerAllClassFileGenerator = new RegisterAllClassFileGenerator(registerSourceFileGenerators, registerSharedPtrSoureFileGenerators, ouputPath);
            foreach (Class @class in classes)
            {
                RegisterSourceFileGenerator registerSourceFileGenerator = new RegisterSourceFileGenerator(ctx, @class, ouputPath);
                registerSourceFileGenerator.save();
                registerSourceFileGenerators.Add(registerSourceFileGenerator);

                RegisterSharedPtrSoureFileGenerator registerSharedPtrSoureFileGenerator = new RegisterSharedPtrSoureFileGenerator(ctx, @class, ouputPath);
                registerSharedPtrSoureFileGenerator.save();
                registerSharedPtrSoureFileGenerators.Add(registerSharedPtrSoureFileGenerator);
            }
            registerAllClassFileGenerator.save();
        }

        private List<Class> findClasses(List<string> classNames)
        {
            List<Class> classes = new List<Class>();
            foreach (string className in classNames)
            {
                classes.AddRange(findClassE(ctx, className));
            }
            return classes;
        }

        private List<Class> findClassE(ASTContext ctx, string name)
        {
            List<Class> classes = new List<Class>();
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
