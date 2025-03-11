cmake_minimum_required(VERSION 3.14)
project(Upsilon.Apps.Passkey.Core VERSION 1.0.0 LANGUAGES CSharp)

# Ensure .NET 8.0 is used
set(CMAKE_DOTNET_TARGET_FRAMEWORK "net8.0")
set(CMAKE_DOTNET_SDK "Microsoft.NET.Sdk")

# Main library project
add_subdirectory(Core)

# Unit tests project
add_subdirectory(UnitTests)
