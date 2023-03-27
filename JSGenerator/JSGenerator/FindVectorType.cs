

using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace JSGenerator
{

    class FindVectorType
    {
        private Dictionary<string, TemplateSpecializationType> templateSpecializationTypes;

        private static readonly Lazy<FindVectorType> lazy = new Lazy<FindVectorType>(() => new FindVectorType());

        public static FindVectorType Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        private FindVectorType()
        {


        }

        public Dictionary<string, TemplateSpecializationType> getTemplateSpecializationTypes()
        {
            return templateSpecializationTypes;
        }

        public bool isStdVector(TemplateSpecializationType templateSpecializationType)
        {
            return templateSpecializationType.Template.LogicalOriginalName == "vector" &&
                templateSpecializationType.Template.Namespace.ToString() == "std";
        }

        public void findAndCacheAllTemplateSpecializationTypes(List<Class> classes)
        {
            Dictionary<string, TemplateSpecializationType> templateSpecializationTypes = new Dictionary<string, TemplateSpecializationType>();
            for (int i = 0; i < classes.Count; i++)
            {
                Class @class = classes[i];
                foreach (var (key, value) in findTemplateSpecializationTypes(@class))
                {
                    templateSpecializationTypes.TryAdd(key, value);
                }
            }
            this.templateSpecializationTypes = templateSpecializationTypes;
        }

        public Dictionary<string, TemplateSpecializationType> findTemplateSpecializationTypes(Class @class)
        {
            List<Method> methods = new List<Method>();
            List<Field> fields = new List<Field>();

            Dictionary<string, TemplateSpecializationType> templateSpecializationTypes = new Dictionary<string, TemplateSpecializationType>();

            List<Method> contructorMethods = RegisterSourceFileGenerator.getSupportContructorMethod(@class);
            List<Method> memberMethods = RegisterSourceFileGenerator.getSupportMemberMethod(@class);
            methods.AddRange(contructorMethods);
            methods.AddRange(memberMethods);
            fields.AddRange(@class.Fields);

            for (int i = 0; i < methods.Count; i++)
            {
                Method method = methods[i];

                for (int j = 0; j < method.Parameters.Count; j++)
                {
                    Parameter parameter = method.Parameters[j];

                    if (parameter.Type is TemplateSpecializationType)
                    {
                        TemplateSpecializationType templateSpecializationType = parameter.Type as TemplateSpecializationType;
                        if (isStdVector(templateSpecializationType))
                        {
                            templateSpecializationTypes.TryAdd($"{templateSpecializationType}", templateSpecializationType);
                        }
                    }
                    else if (parameter.Type is PointerType)
                    {
                        PointerType pointer = parameter.Type as PointerType;
                        if (pointer.Pointee is TemplateSpecializationType)
                        {
                            TemplateSpecializationType templateSpecializationType = pointer.Pointee as TemplateSpecializationType;
                            if (isStdVector(templateSpecializationType))
                            {
                                templateSpecializationTypes.TryAdd($"{templateSpecializationType}", templateSpecializationType);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < fields.Count; i++)
            {
                Field field = fields[i];

                if (field.Type is TemplateSpecializationType)
                {
                    TemplateSpecializationType templateSpecializationType = field.Type as TemplateSpecializationType;
                    if (isStdVector(templateSpecializationType))
                    {
                        templateSpecializationTypes.TryAdd($"{templateSpecializationType}", templateSpecializationType);
                    }
                }
            }

            return templateSpecializationTypes;
        }
    }

}

