using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JSGenerator
{

    public class Config
    {
        public List<string> IncludeDirs { get; set; }
        public List<string> Headers { get; set; }
        public List<string> ClassName { get; set; }
    }

    class SampleLibrary : ILibrary
    {
        private readonly Config config;

        public SampleLibrary(Config config)
        {
            this.config = config;
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath, bool DryRun)
        {
            if (!Directory.Exists(sourcePath))
            {
                throw new Exception($"{sourcePath} Is not an existing directory on disk");
            }
            if (!Directory.Exists(targetPath))
            {
                throw new Exception($"{targetPath} Is not an existing directory on disk");
            }
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                string folder = dirPath.Replace(sourcePath, targetPath);
                if (!Directory.Exists(folder))
                {
                    if (DryRun)
                    {
                        Console.WriteLine($"CreateDirectory: {folder}");
                    }
                    else
                    {
                        Directory.CreateDirectory(folder);
                    }
                }
            }

            foreach (string sourceFilePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string targetFilePath = sourceFilePath.Replace(sourcePath, targetPath);
                if (DryRun)
                {
                    Console.WriteLine($"Copy Flie {sourceFilePath} To {targetFilePath}");
                }
                else
                {
                    File.Copy(sourceFilePath, targetFilePath, true);
                }
            }
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
            //_ = new JSClassRegister(ctx, config.ClassName);

            List<Class> classes = Misc.FindClasses(ctx, config.ClassName);
            //Class classA = classes[0];
            //foreach (var Field in classA.Fields)
            //{
            //    if (Field.QualifiedType.Type is CppSharp.AST.TypedefType)
            //    {
            //        TypedefType t =  (TypedefType)Field.QualifiedType.Type;
            //        Console.WriteLine(t.Declaration.QualifiedOriginalName);
            //    }
            //    if (Field.QualifiedType.Type is CppSharp.AST.BuiltinType)
            //    {
            //        BuiltinType t = (BuiltinType)Field.QualifiedType.Type;
            //        Console.WriteLine(t.Type);
            //    }
            //}

            foreach (var @class in classes)
            {
                RegisterTemplateGenerator Generator = new RegisterTemplateGenerator(ctx, @class);
#if (DEBUG)
                const string OutputFolderPath = "../Debug/Register";
#else
                            const string OutputFolderPath = "../Release/Register";
#endif
                Generator.Save(OutputFolderPath);
            }
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {

        }

        public void Setup(Driver driver)
        {
            DriverOptions options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            options.Quiet = true;
            options.DryRun = true;
            ParserOptions parserOptions = driver.ParserOptions;

            if (!File.Exists(Path.Join(parserOptions.BuiltinsDir, "memory")))
            {
                //parserOptions.NoBuiltinIncludes = true;
                //parserOptions.AddIncludeDirs(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC\14.29.30133\include");
                CopyFilesRecursively(
                    @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC\14.29.30133\include",
                    parserOptions.BuiltinsDir,
                    false);
            }

            Module module = options.AddModule("Sample");
            module.IncludeDirs.AddRange(config.IncludeDirs);
            module.Headers.AddRange(config.Headers);
        }

        public void SetupPasses(Driver driver)
        {
        }
    }

    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            if (args.Length > 0)
            {
                string path = args[0];
                using System.IO.FileStream stream = System.IO.File.OpenRead(path);
                Config config = await JsonSerializer.DeserializeAsync<Config>(stream);
                ConsoleDriver.Run(new SampleLibrary(config));
            }
        }
    }

}
