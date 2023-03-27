using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{
    class VectorGenerator
    {

        private List<Class> classes;

        //private Dictionary<string, TemplateSpecializationType> templateSpecializationTypes;

        public VectorGenerator(List<Class> classes)
        {
            this.classes = classes;
        }

        //public static bool isStdVector(TemplateSpecializationType templateSpecializationType)
        //{
        //    return templateSpecializationType.Template.LogicalOriginalName == "vector" &&
        //        templateSpecializationType.Template.Namespace.ToString() == "std";
        //}

        //public Dictionary<string, TemplateSpecializationType> getTemplateSpecializationTypes()
        //{
        //    return templateSpecializationTypes;
        //}

        string getHeaderFileName(string typeName)
        {
            return $@"CPPVector_{typeName}.h";
        }

        string getSourceFileName(string typeName)
        {
            return $@"CPPVector_{typeName}.cpp";
        }

        string getHeaderFileContent(TemplateSpecializationType templateSpecializationType)
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

        string getSourceFileContent(TemplateSpecializationType templateSpecializationType)
        {
            TemplateArgument templateArgument = templateSpecializationType.Arguments[0];
            string typeName = templateArgument.Type.ToString();

            string ret = $@"
#include ""{getHeaderFileName(typeName)}""

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

        public void save(string outputFolderPath)
        {
            System.IO.Directory.CreateDirectory(outputFolderPath);
            Dictionary<string, TemplateSpecializationType> templateSpecializationTypes = FindVectorType.Instance.getTemplateSpecializationTypes();
            foreach (KeyValuePair<string, TemplateSpecializationType> keyValuePair in templateSpecializationTypes)
            {
                TemplateSpecializationType templateSpecializationType = keyValuePair.Value;
                string typeName = templateSpecializationType.Arguments[0].ToString();
                System.IO.File.WriteAllText(outputFolderPath + "/" + getHeaderFileName(typeName), getHeaderFileContent(templateSpecializationType));
                System.IO.File.WriteAllText(outputFolderPath + "/" + getSourceFileName(typeName), getSourceFileContent(templateSpecializationType));
            }
        }

    }
}
