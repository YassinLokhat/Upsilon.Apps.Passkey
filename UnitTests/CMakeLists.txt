cmake_minimum_required(VERSION 3.14)
project(UnitTests LANGUAGES CSharp)

# Unit tests project
file(GLOB SRC_FILES ${PROJECT_SOURCE_DIR}/*/*.cs)

add_executable(UnitTests
    # Add your test source files here or use a glob pattern
    ${SRC_FILES}
)

# Set properties for the test project
set_target_properties(UnitTests PROPERTIES
    DOTNET_SDK "${CMAKE_DOTNET_SDK}"
    DOTNET_TARGET_FRAMEWORK "${CMAKE_DOTNET_TARGET_FRAMEWORK}"
    VS_DOTNET_REFERENCES "System;System.Core;Microsoft.VisualStudio.TestPlatform.TestFramework"
)

# Add reference from tests to core library
set_property(TARGET UnitTests PROPERTY VS_DOTNET_REFERENCE_Upsilon.Apps.PassKey.Core ${CMAKE_CURRENT_BINARY_DIR}/Core/Core.csproj)
