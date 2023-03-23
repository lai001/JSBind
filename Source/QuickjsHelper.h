#pragma once

 extern "C"
 {
 #include "cutils.h"
 #include "quickjs-libc.h"
 }

 struct QuickjsHelper
 {
	 static int evalFile(JSContext* ctx, const char* filename, int module);
	 static int evalBuffer(JSContext* ctx, const void* buffer, int bufferLength, const char* filename, int evalFlags);
 };
