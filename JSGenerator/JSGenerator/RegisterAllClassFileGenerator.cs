using System;
using System.Collections.Generic;

namespace JSGenerator
{
    class RegisterAllClassFileGenerator
    {
        private readonly List<IRegister> registers;

        public RegisterAllClassFileGenerator(List<IRegister> registers)
        {
            this.registers = registers;
        }

        private string GetHeaderFileName()
        {
            return "AllClassRegister.h";
        }

        private string GetSourceFileName()
        {
            return "AllClassRegister.cpp";
        }

        private string GetHeaderFileContent()
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
                Tuple<string, string, string> values = register.GetRegisterClassCallerContent();
                a += values.Item1 + "\n";
                b += values.Item2 + "\n";
                c += values.Item3 + "\n";
            }

            string content = @$"#include ""{GetHeaderFileName()}""
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

        public void Save(string outputFolderPath)
        {
            System.IO.Directory.CreateDirectory(outputFolderPath);

            System.IO.File.WriteAllText(outputFolderPath + "/" + GetHeaderFileName(), GetHeaderFileContent());
            System.IO.File.WriteAllText(outputFolderPath + "/" + GetSourceFileName(), getSourceFileContent());
        }

    }
}
