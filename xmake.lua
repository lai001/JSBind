set_xmakever("2.7.8")

add_requires("spdlog")
local ThirdParty = ".xmake/ThirdParty"
includes(ThirdParty .. "/Foundation/Foundation")

-- local js_defines = {"CONFIG_BIGNUM", "JS_STRICT_NAN_BOXING"}
local js_defines = {"JS_STRICT_NAN_BOXING"}
        
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

task("setup_project")
    on_run(function ()
        import("devel.git")
        import("core.base.json")
        import("lib.detect.find_program")
        import("core.base.option")

        local vsxmake = "vsxmake2022"
        for _, v in ipairs({"vsxmake2017", "vsxmake2019", "vsxmake2022"}) do
            if option.get(v) then
                vsxmake = v
            end
        end

        local function download(url, branch, name) 
            if os.exists(ThirdParty .. "/" .. name) == false then
                git.clone(url, {depth = 1, branch = branch, outputdir = ThirdParty .. "/" .. name})
            end
        end 
        local function runProgram(programName, argv) 
            local program = find_program(programName)
            if program == programName then
                os.execv(program, argv)
            end            
        end

        os.mkdir(ThirdParty)

        download("https://github.com/bellard/quickjs.git", "master", "quickjs")
        download("https://github.com/c-smile/quickjspp.git", "master", "quickjspp")
        download("https://github.com/lai001/Foundation.git", "main", "Foundation")

        runProgram("xmake", { "project", "-k", vsxmake, "-a", "x64", "-m", "debug" })
        runProgram("cmake", {"-S", "./JSGenerator", "-B", "./JSGenerator/build" })
        runProgram("dotnet", { "sln", "./" .. vsxmake .. "/JSBind.sln", "add", "./JSGenerator/build/JSGenerator.csproj" })

        json.savefile("./JSGenerator/build/Properties/launchSettings.json", {
            profiles = {
                JSGenerator = {
                    commandName = "Project",
                    commandLineArgs = path.join(os.scriptdir(), ".xmake/Generator.json")
                }
            }
        })
    end)

    set_menu {
        usage = "xmake setup_project",
        description = "Setup project.",
        options = {
            {nil, "vsxmake2017", "k", nil, "xmake setup_project --vsxmake2017" },
            {nil, "vsxmake2019", "k", nil, "xmake setup_project --vsxmake2019" },
            {nil, "vsxmake2022", "k", nil, "xmake setup_project --vsxmake2022" },
        }
    }

task("fmt")
    on_run(function ()
        import("lib.detect.find_program")
        local program = find_program("clang-format")
        local function doFormat(folderPath, suffix) 
            -- local style = "Microsoft";
            -- local style = "LLVM";
            local style = "file";
            if os.exists(folderPath) and #os.files(path.join(folderPath, "*." .. suffix)) > 0 then
                os.execv(program, {"-style=" .. style, "-i", path.join(folderPath, "*." .. suffix)})
            end            
        end
        local function clangformatFiles(folderPath) 
            doFormat(folderPath, "hpp")
            doFormat(folderPath, "h")
            doFormat(folderPath, "cpp")
        end        
        if program == "clang-format" then 
            clangformatFiles("Contrast")
            clangformatFiles("Lib")
            clangformatFiles("JSGenerator/build/Register")
            clangformatFiles("JSGenerator/build/Debug/Register")
            clangformatFiles("JSGenerator/build/Release/Register")
        end
    end)

    set_menu {
        usage = "xmake fmt",
        description = "",
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
        local headers = map(os.files("Source/Data/*.h"), function (item)
            return path.join(os.scriptdir(), item)
        end)
        local class_name = map(os.files("Source/Data/*.h"), function (item) 
            return path.basename(item)
        end)
        json.savefile(".xmake/Generator.json", {
            IncludeDirs = includeDirs,
            Headers= headers,
            ClassName = class_name
        })        
        local program = find_program("cmake")
        if program == "cmake" then
            os.execv(program, {"--build", "./JSGenerator/build", "--config Debug"})
            os.execv(program, {"--build", "./JSGenerator/build", "--config Release"})
        end  
        if os.exists("./JSGenerator/build") then
            chdir(os.cd, "./JSGenerator/build/Debug", function () 
                os.execv("./JSGenerator.exe", { path.join(os.scriptdir(), ".xmake/Generator.json") })
            end)
            chdir(os.cd, "./JSGenerator/build/Release", function () 
                os.execv("./JSGenerator.exe", { path.join(os.scriptdir(), ".xmake/Generator.json") })
            end)            
        end     
        task.run("fmt")
    end)

    set_menu {
        usage = "xmake generate_binding",
        description = "Generate binding code.",
        options = {
        }
    }
    
-- target("generate_binding")
--     set_kind("phony")
--     before_build(function (target)
--         import("core.project.task")
--         task.run("generate_binding")
--     end)

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
            add_files(ThirdParty .. "/quickjspp/" .. v)
        end
        for i, v in ipairs(header_files) do 
            add_headerfiles(ThirdParty .. "/quickjspp/" .. v)
        end
        add_includedirs(ThirdParty .. "/quickjspp", {interface = true})
        add_defines(js_defines)
    else 
        add_files(ThirdParty .. "/quickjs/*.c")
        add_headerfiles(ThirdParty .. "/quickjs/*.h")
        remove_files(ThirdParty .. "/quickjs/run-test262.c")
        remove_files(ThirdParty .. "/quickjs/qjsc.c")
        remove_files(ThirdParty .. "/quickjs/qjs.c")
        remove_files(ThirdParty .. "/quickjs/unicode_gen.c")
        add_includedirs(ThirdParty .. "/quickjs", {interface = true})
        add_links("m", "dl", "pthread")
        add_cflags(format([[-D_GNU_SOURCE -DCONFIG_VERSION="%s" -DCONFIG_BIGNUM]], os.date('%Y-%m-%d %H:%M:%S')))
    end

-- target("qjs")
--     set_kind("binary")
--     add_languages("c11")
--     add_deps("quickjs")
--     add_rules("mode.debug", "mode.release")
--     local source_files = {
-- 		"qjs.c",
-- 		"repl.c",
-- 		"qjscalc.c"
-- 	}
--     for i, v in ipairs(source_files) do 
--         add_files(ThirdParty .. "/quickjspp/" .. v)
--     end
--     add_defines(js_defines)
--     set_runargs(os.scriptdir() .. "/main.js")

-- target("qjsc")
--     set_kind("binary")
--     add_rules("mode.debug", "mode.release")
--     add_languages("c11")
--     add_deps("quickjs")
--     add_files(ThirdParty .. "/quickjspp/qjsc.c")
--     add_defines(js_defines)

-- target("JSBind")
--     set_kind("binary")
--     set_runargs(path.join(os.scriptdir(), "Scripts/main.js"))
--     add_languages("c++17", "c11")
--     add_rules("mode.debug", "mode.release")
--     add_files("Source/**.cpp")
--     add_headerfiles("Source/**.h")
--     add_includedirs("Source")
--     local folder = "JSGenerator/build/Debug/Register"
--     if is_mode("release") then
--         folder = "JSGenerator/build/Release/Register"
--     end
--     add_files(folder .. "/*.cpp")
--     add_headerfiles(folder .. "/*.h")
--     add_includedirs(folder)
--     add_deps("quickjs")
--     add_deps("Foundation")
--     add_deps("Lib")
--     add_packages("spdlog")
--     add_defines(js_defines)
--     after_build(function (target) 
--         os.cp("Scripts", target:targetdir())
--     end)

target("Scripts")
    set_kind("phony")
    add_headerfiles("Scripts/*.js")

target("Lib")
    set_kind("static")
    set_symbols("debug")
    add_languages("c++17", "c11")
    add_rules("mode.debug", "mode.release")
    add_files("Lib/*.cpp")
    add_headerfiles("Lib/*.h")
    add_includedirs("Lib", { interface = true })
    add_deps("quickjs")
    add_defines(js_defines)

target("Contrast")
    set_kind("binary")
    set_symbols("debug")
    set_runargs(path.join(os.scriptdir(), "Scripts/main.js"))
    add_languages("c++17", "c11")
    add_rules("mode.debug", "mode.release")
    add_files("Contrast/*.cpp")
    add_files("Source/Data/*.cpp")
    add_headerfiles("Source/Data/*.h")
    add_headerfiles("Contrast/*.h")
    add_includedirs("Contrast")
    add_includedirs("Source")
    add_deps("quickjs")
    add_deps("Lib")
    add_packages("spdlog")
    add_defines(js_defines)
    after_build(function (target) 
        os.cp("Scripts", target:targetdir())
    end)

target("Validation")
    set_kind("binary")
    set_symbols("debug")
    set_runargs(path.join(os.scriptdir(), "Scripts/main.js"))
    add_languages("c++17", "c11")
    add_rules("mode.debug", "mode.release")
    add_files("Validation/*.cpp")
    add_files("Source/Data/*.cpp")
    add_headerfiles("Source/Data/*.h")
    add_headerfiles("Validation/*.h")
    add_includedirs("Validation")
    add_includedirs("Source")
    add_deps("quickjs")
    add_deps("Lib")
    add_deps("Gen")
    add_packages("spdlog")
    add_defines(js_defines)
    after_build(function (target) 
        os.cp("Scripts", target:targetdir())
    end)

target("Gen")
    set_kind("static")
    set_symbols("debug")
    add_languages("c++17", "c11")
    add_rules("mode.debug", "mode.release")
    add_rules("c++.unity_build")
    add_files("JSGenerator/build/Debug/Register/**.cpp")
    add_headerfiles("JSGenerator/build/Debug/Register/**.h")
    add_includedirs("JSGenerator/build/Debug/Register")
    add_includedirs("JSGenerator/build/Debug/", { interface = true })
    add_includedirs("Source")
    add_filegroups("Register", {rootdir="JSGenerator/build/Debug/Register/", files={"**.cpp", "**.h"}})
    add_deps("quickjs")
    add_deps("Lib")
    add_defines(js_defines)