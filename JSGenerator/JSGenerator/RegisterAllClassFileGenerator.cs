using System;
using System.Collections.Generic;

namespace JSGenerator
{
    class RegisterAllClassFileGenerator
    {
        //private List<RegisterSourceFileGenerator> registerSourceFileGenerators;
        //private List<RegisterSharedPtrSoureFileGenerator> registerSharedPtrSoureFileGenerators;
        private List<IRegister> registers;

        public RegisterAllClassFileGenerator(List<IRegister> registers)
        {
            //this.registerSourceFileGenerators = registerSourceFileGenerators;
            //this.registerSharedPtrSoureFileGenerators = registerSharedPtrSoureFileGenerators;
            this.registers = registers;
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
            string content =
@$"#pragma once
extern ""C""
{{
#include ""cutils.h""
#include ""quickjs-libc.h""
}}
typedef int (*AppendSetAllModuleExport)(JSContext* ctx, JSModuleDef* def);
typedef int (*AppendAddAllModuleExport)(JSContext* ctx, JSModuleDef* def);
void registerAllClass(JSContext* ctx, const char* moduleName, AppendSetAllModuleExport set = nullptr, AppendAddAllModuleExport add = nullptr);";
            return content;
        }

        private string getSourceFileContent()
        {
            string a = "";
            string b = "";
            string c = "";

            foreach (IRegister register in registers)
            {
                Tuple<string, string, string> values = register.getRegisterClassCallerContent();
                a += values.Item1 + "\n";
                b += values.Item2 + "\n";
                c += values.Item3 + "\n";
                //string[] s = registerSourceFileGenerator.getRegisterClassCallerContent();
                //b += s[0] + "\n";
                //c += s[1] + "\n";
            }

            //foreach (RegisterSourceFileGenerator registerSourceFileGenerator in registerSourceFileGenerators)
            //{
            //    a += registerSourceFileGenerator.getRegisterClassCallerExternContent() + "\n";
            //    string[] s = registerSourceFileGenerator.getRegisterClassCallerContent();
            //    b += s[0] + "\n";
            //    c += s[1] + "\n";
            //}

            //foreach (RegisterSharedPtrSoureFileGenerator registerSharedPtrSoureFileGenerator in registerSharedPtrSoureFileGenerators)
            //{
            //    a += registerSharedPtrSoureFileGenerator.getRegisterClassCallerExternContent() + "\n";
            //    string[] s = registerSharedPtrSoureFileGenerator.getRegisterClassCallerContent();
            //    b += s[0] + "\n";
            //    c += s[1] + "\n";
            //}

            string content = @$"#include ""{getHeaderFileName()}""
{a}
static AppendSetAllModuleExport appendSetAllModuleExport = nullptr;

static int setAllModuleExport(JSContext* ctx, JSModuleDef* def)
{{
    {b}
    if (appendSetAllModuleExport)
    {{
        appendSetAllModuleExport(ctx, def);
    }}
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

void registerAllClass(JSContext* ctx, const char* moduleName, AppendSetAllModuleExport set, AppendAddAllModuleExport add)
{{
    appendSetAllModuleExport = set;
    if (JSModuleDef *def = JS_NewCModule(ctx, moduleName, js_on_JSModuleInit))
    {{
        addAllModuleExport(ctx, def);
        if (add)
        {{
            add(ctx, def);
        }}
    }}
}}
            ";
            return content;
        }

        public void save(string outputFolderPath)
        {
            System.IO.Directory.CreateDirectory(outputFolderPath);

            System.IO.File.WriteAllText(outputFolderPath + "/" + getHeaderFileName(), getHeaderFileContent());
            System.IO.File.WriteAllText(outputFolderPath + "/" + getSourceFileName(), getSourceFileContent());
        }

    }
}
