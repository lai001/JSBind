using CppSharp.AST;
using System;

namespace JSGenerator
{

    interface IRegister
    {
        public Tuple<string, string, string> getRegisterClassCallerContent();

    }

}