#include <iostream>
#include <memory>
#include <string>

#include "spdlog/spdlog.h"

#include "QuickjsHelper.h"
#include "A.h"
#include "B.h"
#include "BB.h"

void registerClasses(JSContext *ctx, JSValue obj)
{
    jsbind::A::import(ctx, obj, nullptr);
    jsbind::B::import(ctx, obj, nullptr);
    jsbind::BB::import(ctx, obj, nullptr);
}

int main(int argc, char **argv)
{
    assert(argc > 1);
    spdlog::set_level(spdlog::level::trace);
    const std::string file = std::string(argv[1]);

    JSRuntime *rt = JS_NewRuntime();
    JSContext *ctx = JS_NewContext(rt);
    js_std_init_handlers(rt);

    JS_SetModuleLoaderFunc(rt, nullptr, js_module_loader, nullptr);
    js_std_add_helpers(ctx, argc, argv);
    js_init_module_std(ctx, "std");
    js_init_module_os(ctx, "os");

    JSValue globalObj = JS_GetGlobalObject(ctx);

    registerClasses(ctx, globalObj);
    JS_FreeValue(ctx, globalObj);

    QuickjsHelper::evalFile(ctx, file.c_str(), JS_EVAL_TYPE_MODULE);

    JS_RunGC(rt);
    js_std_free_handlers(rt);
    JS_FreeContext(ctx);
    JS_FreeRuntime(rt);
    return 0;
}