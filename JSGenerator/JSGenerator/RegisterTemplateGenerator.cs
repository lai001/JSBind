using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{

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
            string className = @class.Name;
            string headerFilePath = @class.TranslationUnit.IncludePath;

            string ret = $@"
#pragma once
#include ""{headerFilePath}""
#include ""_QuickjsHelper.h""

JSClassID get_js_{className}_class_id();
";
            return ret;
        }

        public void Save(string outputFolderPath)
        {
            string className = @class.Name;
            string fileName = $"Class{className}Register.cpp";
            string headerFileName = $"Class{className}Register.h";
            System.IO.Directory.CreateDirectory(outputFolderPath);
            System.IO.File.WriteAllText(outputFolderPath + "/" + headerFileName, GetHeaderFileContent());
        }

        public Tuple<string, string, string> GetRegisterClassCallerContent()
        {
            throw new NotImplementedException();
        }
    }

}
