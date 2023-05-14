using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{
    class JSClassRegister
    {
        private readonly ASTContext ctx;

        public JSClassRegister(ASTContext ctx, List<string> classNames)
        {
            this.ctx = ctx;
            List<Class> classes = FindClasses(classNames);
            FindVectorType.Instance.FindAndCacheAllTemplateSpecializationTypes(classes);
            VectorGenerator vectorGenerator = new VectorGenerator(classes);
            List<RegisterSourceFileGenerator> registerSourceFileGenerators = new List<RegisterSourceFileGenerator>();
            List<CPPVectorRegisterGenerator> cppVectorRegisterGenerators = new List<CPPVectorRegisterGenerator>();
            List<RegisterSharedPtrSoureFileGenerator> registerSharedPtrSoureFileGenerators = new List<RegisterSharedPtrSoureFileGenerator>();

            foreach (Class @class in classes)
            {
                RegisterSourceFileGenerator registerSourceFileGenerator = new RegisterSourceFileGenerator(ctx, @class);
                registerSourceFileGenerators.Add(registerSourceFileGenerator);
                RegisterSharedPtrSoureFileGenerator registerSharedPtrSoureFileGenerator = new RegisterSharedPtrSoureFileGenerator(ctx, @class);
                registerSharedPtrSoureFileGenerators.Add(registerSharedPtrSoureFileGenerator);
            }
            
            foreach (TemplateSpecializationType templateSpecializationType in FindVectorType.Instance.GetTemplateSpecializationTypes().Values)
            {
                CPPVectorRegisterGenerator cppVectorRegisterGenerator = new CPPVectorRegisterGenerator(templateSpecializationType);
                cppVectorRegisterGenerators.Add(cppVectorRegisterGenerator);
            }

            List<IRegister> registers = new List<IRegister>();
            registers.AddRange(registerSourceFileGenerators);
            registers.AddRange(cppVectorRegisterGenerators);
            registers.AddRange(registerSharedPtrSoureFileGenerators);
            RegisterAllClassFileGenerator registerAllClassFileGenerator = new RegisterAllClassFileGenerator(registers);

#if (DEBUG)
            const string outputFolderPath = "../Debug/Register";
#else
            const string outputFolderPath = "../Release/Register";
#endif
            vectorGenerator.Save(outputFolderPath);
            foreach (CPPVectorRegisterGenerator cppVectorRegisterGenerator in cppVectorRegisterGenerators)
            {
                cppVectorRegisterGenerator.Save(outputFolderPath);
            }
            for (int i = 0; i < classes.Count; i++)
            {
                registerSourceFileGenerators[i].Save(outputFolderPath);
                registerSharedPtrSoureFileGenerators[i].Save(outputFolderPath);
            }
            registerAllClassFileGenerator.Save(outputFolderPath);
            new HelperGenerator().Save(outputFolderPath);
        }

        private List<Class> FindClasses(List<string> classNames)
        {
            List<Class> classes = new List<Class>();
            foreach (string className in classNames)
            {
                classes.AddRange(FindClassE(ctx, className));
            }
            return classes;
        }

        private List<Class> FindClassE(ASTContext ctx, string name)
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
