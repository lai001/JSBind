using System.Collections.Generic;

namespace JSGenerator
{
    class RegisterAllClassFileGenerator
    {
        private List<RegisterSourceFileGenerator> registerSourceFileGenerators;
        private List<RegisterSharedPtrSoureFileGenerator> registerSharedPtrSoureFileGenerators;

        private string outputPath;

        public RegisterAllClassFileGenerator(List<RegisterSourceFileGenerator> registerSourceFileGenerators,
            List<RegisterSharedPtrSoureFileGenerator> registerSharedPtrSoureFileGenerators,
            string outputPath)
        {
            this.outputPath = outputPath;
            this.registerSourceFileGenerators = registerSourceFileGenerators;
            this.registerSharedPtrSoureFileGenerators = registerSharedPtrSoureFileGenerators;
        }

        private string getHeaderFileName()
        {
            return "AllClassRegister.h";
        }

        private string getSourceFileName()
        {
            return "AllClassRegister.cpp";
        }

        private string getHeaderFileContent()
        {
            string content = @$"#pragma once
extern ""C""
{{
#include ""cutils.h""
#include ""quickjs-libc.h""
}}
void registerAllClass(JSContext* ctx, const char* moduleName);";
            return content;
        }

        private string getSourceFileContent()
        {
            string a = "";
            string b = "";
            string c = "";

            foreach (RegisterSourceFileGenerator registerSourceFileGenerator in registerSourceFileGenerators)
            {
                a += registerSourceFileGenerator.getRegisterClassCallerExternContent() + "\n";
                string[] s = registerSourceFileGenerator.getRegisterClassCallerContent();
                b += s[0] + "\n";
                c += s[1] + "\n";
            }

            foreach (RegisterSharedPtrSoureFileGenerator registerSharedPtrSoureFileGenerator in registerSharedPtrSoureFileGenerators)
            {
                a += registerSharedPtrSoureFileGenerator.getRegisterClassCallerExternContent() + "\n";
                string[] s = registerSharedPtrSoureFileGenerator.getRegisterClassCallerContent();
                b += s[0] + "\n";
                c += s[1] + "\n";
            }

            string content = @$"#include ""{getHeaderFileName()}""
{a}
static int setAllModuleExport(JSContext* ctx, JSModuleDef* def)
{{
    {b}
	return 0;
}}

static void addAllModuleExport(JSContext* ctx, JSModuleDef* def)
{{
    {c}
}}

static int js_on_JSModuleInit(JSContext* ctx, JSModuleDef* def)
{{
	return setAllModuleExport(ctx, def);
}}

void registerAllClass(JSContext* ctx, const char* moduleName)
{{
 	if (JSModuleDef* def = JS_NewCModule(ctx, moduleName, js_on_JSModuleInit))
	{{
        addAllModuleExport(ctx, def);
	}}
}}
            ";
            return content;
        }

        public void save()
        {
            System.IO.Directory.CreateDirectory(outputPath);

            System.IO.File.WriteAllText(outputPath + "/" + getHeaderFileName(), getHeaderFileContent());
            System.IO.File.WriteAllText(outputPath + "/" + getSourceFileName(), getSourceFileContent());
        }

    }
}
