cmake_minimum_required(VERSION 3.23.1)
project(JSGenerator VERSION 0.1.0 LANGUAGES CSharp)
set(TARGET_NAME "JSGenerator")

set(CMAKE_CSharp_FLAGS "/langversion:latest")

file(GLOB SOURCE_FILES "JSGenerator/*.cs")

add_executable(${TARGET_NAME}
    ${SOURCE_FILES}
)

set_target_properties(${TARGET_NAME} PROPERTIES
    DOTNET_SDK "Microsoft.NET.Sdk"
    DOTNET_TARGET_FRAMEWORK "netcoreapp3.1"
    VS_PACKAGE_REFERENCES "CppSharp_1.0.1"
    DOTNET_STARTUP_OBJECT "JSGenerator.Program"
)
