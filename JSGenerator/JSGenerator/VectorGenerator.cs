using CppSharp.AST;
using System.Collections.Generic;

namespace JSGenerator
{
    class VectorGenerator
    {

        private readonly List<Class> classes;

        public VectorGenerator(List<Class> classes)
        {
            this.classes = classes;
        }

        string GetHeaderFileName(string typeName)
        {
            return $@"CPPVector_{typeName}.h";
        }

        string GetSourceFileName(string typeName)
        {
            return $@"CPPVector_{typeName}.cpp";
        }

        string GetHeaderFileContent(TemplateSpecializationType templateSpecializationType)
        {
            TemplateArgument templateArgument = templateSpecializationType.Arguments[0];
            string typeName = templateArgument.Type.ToString();

            string ret = $@"
#pragma once

#include <functional>
#include <vector>

struct CPPVector_{typeName}
{{
    std::function<std::vector<{typeName}>*()> getVector;

	void set(const size_t index, const {typeName}& value);

	{typeName} at(const size_t index) const;
}};

";
            return ret;
        }

        string GetSourceFileContent(TemplateSpecializationType templateSpecializationType)
        {
            TemplateArgument templateArgument = templateSpecializationType.Arguments[0];
            string typeName = templateArgument.Type.ToString();

            string ret = $@"
#include ""{GetHeaderFileName(typeName)}""

void CPPVector_{typeName}::set(const size_t index, const {typeName}& value)
{{
    std::vector<{typeName}>& vec = *getVector();
    vec[index] = value;
}}

{typeName} CPPVector_{typeName}::at(const size_t index) const
{{
    const std::vector<{typeName}>& vec = *getVector();
    return vec.at(index);
}}
";
            return ret;
        }

        public void Save(string outputFolderPath)
        {
            System.IO.Directory.CreateDirectory(outputFolderPath);
            Dictionary<string, TemplateSpecializationType> templateSpecializationTypes = FindVectorType.Instance.GetTemplateSpecializationTypes();
            foreach (KeyValuePair<string, TemplateSpecializationType> keyValuePair in templateSpecializationTypes)
            {
                TemplateSpecializationType templateSpecializationType = keyValuePair.Value;
                string typeName = templateSpecializationType.Arguments[0].ToString();
                System.IO.File.WriteAllText(outputFolderPath + "/" + GetHeaderFileName(typeName), GetHeaderFileContent(templateSpecializationType));
                System.IO.File.WriteAllText(outputFolderPath + "/" + GetSourceFileName(typeName), GetSourceFileContent(templateSpecializationType));
            }
        }

    }
}
