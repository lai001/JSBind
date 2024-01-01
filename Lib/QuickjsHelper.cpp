#include "QuickjsHelper.h"
extern "C"
{
#include "cutils.h"
}

int QuickjsHelper::evalFile(JSContext *ctx, const char *filename, int module)
{
    uint8_t *buf;
    int ret, eval_flags;
    size_t buf_len;

    buf = js_load_file(ctx, &buf_len, filename);
    if (!buf)
    {
        perror(filename);
        exit(1);
    }

    if (module < 0)
    {
        module = (has_suffix(filename, ".mjs") || JS_DetectModule((const char *)buf, buf_len));
    }
    if (module)
    {
        eval_flags = JS_EVAL_TYPE_MODULE;
    }
    else
    {
        eval_flags = JS_EVAL_TYPE_GLOBAL;
    }
    ret = evalBuffer(ctx, buf, buf_len, filename, eval_flags);
    js_free(ctx, buf);
    return ret;
}

int QuickjsHelper::evalBuffer(JSContext *ctx, const void *buffer, int bufferLength, const char *filename, int evalFlags)
{
    JSValue val;
    int ret;

    if ((evalFlags & JS_EVAL_TYPE_MASK) == JS_EVAL_TYPE_MODULE)
    {
        val = JS_Eval(ctx, static_cast<const char *>(buffer), bufferLength, filename,
                      evalFlags | JS_EVAL_FLAG_COMPILE_ONLY);
        if (!JS_IsException(val))
        {
            js_module_set_import_meta(ctx, val, TRUE, TRUE);
            val = JS_EvalFunction(ctx, val);
        }
    }
    else
    {
        val = JS_Eval(ctx, static_cast<const char *>(buffer), bufferLength, filename, evalFlags);
    }
    if (JS_IsException(val))
    {
        js_std_dump_error(ctx);
        ret = -1;
    }
    else
    {
        ret = 0;
    }
    JS_FreeValue(ctx, val);
    return ret;
}
