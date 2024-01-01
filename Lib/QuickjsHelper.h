#pragma once
#include <assert.h>
extern "C"
{
#include "quickjs-libc.h"
}

struct QuickjsHelper
{
    static int evalFile(JSContext *ctx, const char *filename, int module);
    static int evalBuffer(JSContext *ctx, const void *buffer, int bufferLength, const char *filename, int evalFlags);

    template <typename T, typename U> static T *getNativeObject(JSValue this_val)
    {
        void *p = JS_GetOpaque(this_val, U::classID);
        assert(p);
        T *nativeObject = reinterpret_cast<T *>(p);
        assert(nativeObject);
        return nativeObject;
    }
};
