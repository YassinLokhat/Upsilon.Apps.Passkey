cmake_minimum_required(VERSION 3.14)
project(Upsilon.Apps.Passkey.Core VERSION 1.0.0 LANGUAGES CSharp)

# Ensure .NET 8.0 is used
set(CMAKE_DOTNET_TARGET_FRAMEWORK "net8.0")
set(CMAKE_DOTNET_SDK "Microsoft.NET.Sdk")

set(CMAKE_SUPPRESS_REGENERATION ON)

set(CMAKE_CSharp_FLAGS_DEBUG "/warn:9999" CACHE STRING "" FORCE)
set(CMAKE_CSharp_FLAGS_RELEASE "/warn:9999" CACHE STRING "" FORCE)

# Main library project
add_subdirectory(Core)

# Unit tests project
add_subdirectory(UnitTests)
