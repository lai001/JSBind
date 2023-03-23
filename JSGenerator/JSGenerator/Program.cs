using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using System.Collections.Generic;
using System.Text.Json;

namespace JSGenerator
{

    public class Config
    {
        public List<string> IncludeDirs { get; set; }
        public List<string> Headers { get; set; }
        public List<string> ClassName { get; set; }
    }

    class Program
    {
        private class SampleLibrary : ILibrary
        {
            private Config config;

            public SampleLibrary(Config config)
            {
                this.config = config;
            }

            public void Postprocess(Driver driver, ASTContext ctx)
            {

                JSClassRegister luaClassRegister = new JSClassRegister(ctx, config.ClassName);
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
                Module module = options.AddModule("Sample");
                module.IncludeDirs.AddRange(config.IncludeDirs);
                module.Headers.AddRange(config.Headers);
            }

            public void SetupPasses(Driver driver)
            {
            }
        }

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
