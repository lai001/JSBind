#include <iostream>
#include <memory>
#include <string>
#include "spdlog/spdlog.h"
#include "QuickjsHelper.h"
#include "AllClassRegister.h"
#include "ClassBRegister.h"
#include "SharedPtrClassBRegister.h"

void preImport(JSContext* ctx)
{
	char* content = R"(import * as ks from "ks";)";
	if (JS_IsException(JS_Eval(ctx, content, strlen(content), "import", JS_EVAL_TYPE_MODULE)))
	{
		assert(false);
	}
}

void pushValue(JSContext* ctx, char* name = "g_b", B* instance = nullptr)
{
	JSValue globalObject = JS_GetGlobalObject(ctx);
	JSValue object = JS_NewObjectClass(ctx, get_js_B_class_id());
	JSWrapperB* wrapper = JSWrapperB::UnretainedSetOpaque(object);
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

void pushSharedValue(JSContext* ctx, char* name = "g_sharedptr_b", std::shared_ptr<B> instance = nullptr)
{
	JSValue globalObject = JS_GetGlobalObject(ctx);
	JSValue object = JS_NewObjectClass(ctx, get_js_SharedPtrB_class_id());
	JSWrapperSharedPtrB* wrapper = JSWrapperSharedPtrB::UnretainedSetOpaque(object);
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

int main(int argc, char** argv)
{
	assert(argc > 1);
	spdlog::set_level(spdlog::level::trace);
	const std::string file = std::string(argv[1]);

	JSRuntime* rt = JS_NewRuntime();
	JSContext* ctx = JS_NewContext(rt);
	js_std_init_handlers(rt);
	
	JS_SetModuleLoaderFunc(rt, nullptr, js_module_loader, nullptr);
	js_std_add_helpers(ctx, argc, argv);
	js_init_module_std(ctx, "std");
	js_init_module_os(ctx, "os");

	registerAllClass(ctx, "ks");
	preImport(ctx);
	B* b = new B();
	std::shared_ptr<B> sharedptr = std::make_shared<B>();
	pushValue(ctx);
	pushValue(ctx, "g_b1", b);

	pushSharedValue(ctx);
	pushSharedValue(ctx, "g_sharedptr_b1", sharedptr);
	sharedptr->str = "sharedptr: " + sharedptr->str;
	QuickjsHelper::evalFile(ctx, file.c_str(), JS_EVAL_TYPE_MODULE);
	
	js_std_free_handlers(rt);
	JS_FreeContext(ctx);
	JS_FreeRuntime(rt);
	delete b;
	return 0;
}
