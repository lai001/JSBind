#include <iostream>
#include <memory>
#include <string>

#include "spdlog/spdlog.h"

#include "QuickjsHelper.h"
#include "A.h"
#include "View.h"
#include "Button.h"
#include "ImageView.h"
#include "Data/Button.h"

void registerClasses(JSContext *ctx, JSValue obj)
{
    jsbind::A::import(ctx, obj, nullptr);
    jsbind::View::import(ctx, obj, nullptr);
    jsbind::Button::import(ctx, obj, nullptr);
    jsbind::ImageView::import(ctx, obj, nullptr);
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
    {
        ::Button *button = new Button();
        JS_SetPropertyStr(ctx, globalObj, "nativeView", jsbind::View::setNativeObjectPointer(ctx, button, true));
    }
    JS_FreeValue(ctx, globalObj);

    QuickjsHelper::evalFile(ctx, file.c_str(), JS_EVAL_TYPE_MODULE);

    js_std_free_handlers(rt);
    JS_FreeContext(ctx);
    JS_FreeRuntime(rt);
    return 0;
}