cmake_minimum_required(VERSION 3.14)
project(Core LANGUAGES CSharp)

# Main library project
file(GLOB SRC_FILES ${PROJECT_SOURCE_DIR}/*/*.cs)

add_library(Core SHARED
    # Add your source files here or use a glob pattern
    ${SRC_FILES}
)

# Set properties for the library
set_target_properties(Core PROPERTIES
    DOTNET_SDK "${CMAKE_DOTNET_SDK}"
    DOTNET_TARGET_FRAMEWORK "${CMAKE_DOTNET_TARGET_FRAMEWORK}"
    VS_DOTNET_REFERENCES "System;System.Core;System.Data;System.Security;System.Text.Json"
)

set_property(TARGET Core PROPERTY VS_GLOBAL_ImplicitUsings "enable")
set_property(TARGET Core PROPERTY VS_GLOBAL_Nullable "enable")
set_property(TARGET Core PROPERTY VS_GLOBAL_Title "$(AssemblyName)")
set_property(TARGET Core PROPERTY VS_GLOBAL_Authors "Yassin Lokhat")
set_property(TARGET Core PROPERTY VS_GLOBAL_Configurations "Debug;Release")
set_property(TARGET Core PROPERTY VS_GLOBAL_Description "A local stored Password Manager Core.")
set_property(TARGET Core PROPERTY VS_GLOBAL_Version "1.0.0")

set_property(TARGET Core PROPERTY VS_GLOBAL_WarningLevel_DEBUG "9999")
set_property(TARGET Core PROPERTY VS_GLOBAL_TreatWarningsAsErrors "True")