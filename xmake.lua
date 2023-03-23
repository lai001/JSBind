add_requires("spdlog")

local js_defines = {"CONFIG_BIGNUM", "JS_STRICT_NAN_BOXING"}
        
local function chdir(cd, path, caller) 
    local oldir = cd(path)
    caller()
    cd(oldir)
end

local function map(self, f)
    local t = {}
    for i, v in ipairs(self) do
        t[i] = f(v)
    end
    return t
end

task("project_setup")
    on_run(function ()
        import("devel.git")
        import("core.base.json")
        import("lib.detect.find_program")
        if os.exists("./ThirdParty") == false then
            os.mkdir("./ThirdParty")
        end
        if os.exists("./ThirdParty/quickjs") == false then
            git.clone("https://github.com/bellard/quickjs.git", {depth = 1, branch = "master", outputdir = "./ThirdParty/quickjs"})
        end
        if os.exists("./ThirdParty/quickjspp") == false then
            git.clone("https://github.com/c-smile/quickjspp.git", {depth = 1, branch = "master", outputdir = "./ThirdParty/quickjspp"})
        end
        local program = find_program("cmake")
        if program == "cmake" then
            if os.exists("./JSGenerator/build") == false then
                os.execv(program, {"-S", "./JSGenerator", "-B", "./JSGenerator/build"})
            end
        end

        if os.exists("./JSGenerator/build/Properties/launchSettings.json") == false then
            json.savefile("./JSGenerator/build/Properties/launchSettings.json", {
                profiles = {
                    JSGenerator = {
                        commandName = "Project",
                        commandLineArgs = path.join(os.scriptdir(), ".xmake/Generator.json")
                    }
                }
            })
        end        
    end)

    set_menu {
        usage = "xmake project_setup",
        description = "Setup project.",
        options = {
        }
    }

task("clang_format")
    on_run(function ()
        import("lib.detect.find_program")
        local program = find_program("clang-format")
        if program == "clang-format" and os.exists("JSGenerator/build/Register") then 
            os.execv(program, {"-style=Microsoft", "-i", 
            path.join(os.scriptdir(), "JSGenerator/build/Register/*.cpp"),
            path.join(os.scriptdir(), "JSGenerator/build/Register/*.h") })
        end
    end)

    set_menu {
        usage = "xmake clang_format",
        description = "Format binding code.",
        options = {
        }
    }

task("generate_binding")
    on_run(function ()
        import("lib.detect.find_program")
        import("core.project.task")
        import("core.base.json")
        local includeDirs = map({
            "Source",
        }, function (item)
            return path.join(os.scriptdir(), item)
        end)
        local headers = map({
            "A.h",
            "B.h"
        }, function (item)
            return path.join(os.scriptdir(), "Source", item)
        end)
        local class_name = {"A", "B"}
        json.savefile(".xmake/Generator.json", {
            IncludeDirs = includeDirs,
            Headers= headers,
            ClassName = class_name
        })        
        local program = find_program("cmake")
        if program == "cmake" then
            if os.exists("./JSGenerator/build") then
                os.execv(program, {"--build", "./JSGenerator/build"})
            end
        end  
        if os.exists("./JSGenerator/build") then
            chdir(os.cd, "./JSGenerator/build/Debug", function () 
                os.execv("./JSGenerator.exe", { path.join(os.scriptdir(), ".xmake/Generator.json") })
            end)
        end     
        task.run("clang_format")
    end)

    set_menu {
        usage = "xmake generate_binding",
        description = "Generate binding code.",
        options = {
        }
    }
    
target("generate_binding")
    set_kind("phony")
    before_build(function (target)
        import("core.project.task")
        task.run("generate_binding")
    end)

target("quickjs")
    set_kind("static")
    add_languages("c11")
    add_rules("mode.debug", "mode.release")
    if is_plat("windows") then
        local source_files = {
            "cutils.c",
            "libregexp.c",
            "libunicode.c",
            "quickjs.c",
            "quickjs-libc.c",
            "libbf.c",
        }
        local header_files = {
            "cutils.h",
            "libregexp.h",
            "libregexp-opcode.h",
            "libunicode.h",
            "libunicode-table.h",
            "list.h",
            "quickjs.h",
            "quickjs-atom.h",
            "quickjs-libc.h",
            "quickjs-opcode.h",
            "quickjs-jsx.h",
	    }
        for i, v in ipairs(source_files) do 
            add_files("ThirdParty/quickjspp/" .. v)
        end
        for i, v in ipairs(header_files) do 
            add_headerfiles("ThirdParty/quickjspp/" .. v)
        end
        add_includedirs("ThirdParty/quickjspp", {interface = true})
        add_defines(js_defines)
    else 
        add_files("ThirdParty/quickjs/*.c")
        add_headerfiles("ThirdParty/quickjs/*.h")
        remove_files("ThirdParty/quickjs/run-test262.c")
        remove_files("ThirdParty/quickjs/qjsc.c")
        remove_files("ThirdParty/quickjs/qjs.c")
        remove_files("ThirdParty/quickjs/unicode_gen.c")
        add_includedirs("ThirdParty/quickjs", {interface = true})
        add_links("m", "dl", "pthread")
        add_cflags(format([[-D_GNU_SOURCE -DCONFIG_VERSION="%s" -DCONFIG_BIGNUM]], os.date('%Y-%m-%d %H:%M:%S')))
    end

target("qjs")
    set_kind("binary")
    add_languages("c11")
    add_deps("quickjs")
    add_rules("mode.debug", "mode.release")
    local source_files = {
		"qjs.c",
		"repl.c",
		"qjscalc.c"
	}
    for i, v in ipairs(source_files) do 
        add_files("ThirdParty/quickjspp/" .. v)
    end
    add_defines(js_defines)
    set_runargs(os.scriptdir() .. "/main.js")

target("qjsc")
    set_kind("binary")
    add_rules("mode.debug", "mode.release")
    add_languages("c11")
    add_deps("quickjs")
    add_files("ThirdParty/quickjspp/qjsc.c")
    add_defines(js_defines)

target("JSBind")
    set_kind("binary")
    set_runargs(path.join(os.scriptdir(), "Scripts/main.js"))
    add_languages("c++17", "c11")
    add_rules("mode.debug", "mode.release")
    add_files("Source/*.cpp")
    add_headerfiles("Source/*.h")
    add_files("JSGenerator/build/Register/*.cpp")
    add_headerfiles("JSGenerator/build/Register/*.h")
    add_includedirs("JSGenerator/build/Register")
    add_includedirs("Source")
    add_deps("quickjs")
    add_packages("spdlog")
    add_defines(js_defines)

target("Scripts")
    set_kind("phony")
    add_headerfiles("Scripts/*.js")
