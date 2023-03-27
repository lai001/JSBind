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
            this.ctx = ctx;
            List<Class> classes = findClasses(classNames);
            FindVectorType.Instance.findAndCacheAllTemplateSpecializationTypes(classes);
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
            
            foreach (TemplateSpecializationType templateSpecializationType in FindVectorType.Instance.getTemplateSpecializationTypes().Values)
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
            vectorGenerator.save(outputFolderPath);
            foreach (CPPVectorRegisterGenerator cppVectorRegisterGenerator in cppVectorRegisterGenerators)
            {
                cppVectorRegisterGenerator.save(outputFolderPath);
            }
            for (int i = 0; i < classes.Count; i++)
            {
                registerSourceFileGenerators[i].save(outputFolderPath);
                registerSharedPtrSoureFileGenerators[i].save(outputFolderPath);
            }
            registerAllClassFileGenerator.save(outputFolderPath);
            new HelperGenerator().save(outputFolderPath);
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
