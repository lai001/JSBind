#ifndef Contrast_BB_H
#define Contrast_BB_H

#include <vector>
extern "C"
{
#include "quickjs.h"
}

namespace jsbind
{
struct BB
{
    static JSClassID classID;
    static JSClassDef classDef;
    static std::vector<JSCFunctionListEntry> classProtoFuncs;
    static JSValue import(JSContext *ctx, JSValue obj, JSModuleDef *moduleDef);
};
} // namespace jsbind

#endif
