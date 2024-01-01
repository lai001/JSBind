#ifndef Contrast_View_H
#define Contrast_View_H

#include <vector>
extern "C"
{
#include "quickjs.h"
}
#include "Data/View.h"

namespace jsbind
{
struct View
{
    static JSClassID classID;
    static JSClassDef classDef;
    static std::vector<JSCFunctionListEntry> classProtoFuncs;
    static JSValue import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef);
    static JSValue setNativeObjectPointer(JSContext *ctx, ::View *nativeObject, const bool isManaged);
};
} // namespace jsbind

#endif
