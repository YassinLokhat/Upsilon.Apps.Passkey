cmake_minimum_required(VERSION 3.14)
project(UnitTests LANGUAGES CSharp)

# Unit tests project
file(GLOB_RECURSE SRC_FILES 
    "${CMAKE_CURRENT_SOURCE_DIR}/*.cs"
)
file(GLOB_RECURSE EXCLUDED_FILES
    "${CMAKE_CURRENT_SOURCE_DIR}/obj/*"
    "${CMAKE_CURRENT_SOURCE_DIR}/bin/*"
)
if(EXCLUDED_FILES)
    list(REMOVE_ITEM SRC_FILES ${EXCLUDED_FILES})
endif()

add_executable(UnitTests
    # Add your test source files here or use a glob pattern
    ${SRC_FILES}
)

# Set properties for the test project
set_target_properties(UnitTests PROPERTIES
    DOTNET_SDK "${CMAKE_DOTNET_SDK}"
    DOTNET_TARGET_FRAMEWORK "${CMAKE_DOTNET_TARGET_FRAMEWORK}"
	VS_PACKAGE_REFERENCES "FluentAssertions_7.0.0;Microsoft.NET.Test.Sdk_17.11.1;MSTest_3.6.1"
	VS_GLOBAL_ImplicitUsings "enable"
)

# Add reference from tests to core library
target_link_libraries(UnitTests Core)
