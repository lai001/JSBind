#include <iostream>
#include <memory>
#include <string>

#include "Foundation/Foundation.hpp"
#include "spdlog/spdlog.h"

#include "AllClassRegister.h"
#include "ClassBRegister.h"
#include "QuickjsHelper.h"
#include "SharedPtrClassBRegister.h"

static int setAllModuleExport(JSContext *ctx, JSModuleDef *def)
{
    return 0;
}

static int addAllModuleExport(JSContext *ctx, JSModuleDef *def)
{
    return 0;
}

void preImport(JSContext *ctx)
{
    char *content = R"(import * as ks from "ks";)";
    if (JS_IsException(JS_Eval(ctx, content, strlen(content), "import", JS_EVAL_TYPE_MODULE)))
    {
        assert(false);
    }
}

void pushValue(JSContext *ctx, char *name = "g_b", B *instance = nullptr)
{
    JSValue globalObject = JS_GetGlobalObject(ctx);
    JSValue object = JS_NewObjectClass(ctx, get_js_B_class_id());
    JSWrapperB *wrapper = JSWrapperB::UnretainedSetOpaque(object);
    if (instance)
    {
        wrapper->instance = instance;
        wrapper->HostType = EMemoryHostType::Cpp;
    }
    else
    {
        wrapper->instance = new B();
    }
    JS_SetPropertyStr(ctx, globalObject, name, object);
    JS_FreeValue(ctx, globalObject);
}

void pushSharedValue(JSContext *ctx, char *name = "g_sharedptr_b", std::shared_ptr<B> instance = nullptr)
{
    JSValue globalObject = JS_GetGlobalObject(ctx);
    JSValue object = JS_NewObjectClass(ctx, get_js_SharedPtrB_class_id());
    JSWrapperSharedPtrB *wrapper = JSWrapperSharedPtrB::UnretainedSetOpaque(object);
    if (instance)
    {
        wrapper->instance = instance;
    }
    else
    {
        wrapper->instance = std::make_shared<B>();
    }
    JS_SetPropertyStr(ctx, globalObject, name, object);
    JS_FreeValue(ctx, globalObject);
}

int fib(int n)
{
    if (n <= 0)
    {
        return 0;
    }
    if (n < 3)
        return 1;
    return fib(n - 1) + fib(n - 2);
}

void test()
{
    std::function<long long()> getTime = []() {
        std::chrono::steady_clock::time_point now = std::chrono::high_resolution_clock::now();
        long long currentMilliseconds =
            std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()).count();
        return currentMilliseconds;
    };
    const long long time = getTime();
    int a = 0;
    for (int i = 0; i < 45; i++)
    {
        a = fib(i);
    }
    std::cout << getTime() - time << std::endl;
    std::cout << a << std::endl;
}

int main(int argc, char **argv)
{
    assert(argc > 1);
    spdlog::set_level(spdlog::level::trace);
    const std::string file = std::string(argv[1]);

    JSRuntime *rt = JS_NewRuntime();
    JSContext *ctx = JS_NewContext(rt);
    js_std_init_handlers(rt);

    defer
    {
        js_std_free_handlers(rt);
        JS_FreeContext(ctx);
        JS_FreeRuntime(rt);
    };

    JS_SetModuleLoaderFunc(rt, nullptr, js_module_loader, nullptr);
    js_std_add_helpers(ctx, argc, argv);
    js_init_module_std(ctx, "std");
    js_init_module_os(ctx, "os");

    registerAllClass(ctx, "ks", setAllModuleExport, addAllModuleExport);
    preImport(ctx);
    B *b = new B();
    defer
    {
        delete b;
    };
    std::shared_ptr<B> sharedptr = std::make_shared<B>();
    pushValue(ctx);
    pushValue(ctx, "g_b1", b);

    pushSharedValue(ctx);
    pushSharedValue(ctx, "g_sharedptr_b1", sharedptr);
    sharedptr->str = "sharedptr: " + sharedptr->str;
    QuickjsHelper::evalFile(ctx, file.c_str(), JS_EVAL_TYPE_MODULE);

    {
        JSValue globalObject = JS_GetGlobalObject(ctx);
        defer
        {
            JS_FreeValue(ctx, globalObject);
        };
        JSAtom atom = JS_NewAtom(ctx, "array");
        defer
        {
            JS_FreeAtom(ctx, atom);
        };
        if (JS_HasProperty(ctx, globalObject, atom) > 0)
        {
            JSValue arrayValue = JS_GetPropertyStr(ctx, globalObject, "array");
            defer
            {
                JS_FreeValue(ctx, arrayValue);
            };
            if (JS_IsArray(ctx, arrayValue))
            {
                JSValue *header;
                uint32_t countp;
                bool status = JS_GetFastArray(ctx, arrayValue, &header, &countp);
                for (uint32_t i = 0; i < countp; i++)
                {
                    JSValue element = header[i];
                    int value;
                    bool result = JS_ToInt32(ctx, &value, element) == 0;
                    spdlog::debug("{}, {}", result, value);
                }
            }
        }
    }

    return 0;
}
