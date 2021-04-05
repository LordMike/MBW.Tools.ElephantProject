# MBW.Tools.ElephantProject [![Generic Build](https://github.com/LordMike/MBW.Tools.ElephantProject/actions/workflows/dotnet.yml/badge.svg)](https://github.com/LordMike/MBW.Tools.ElephantProject/actions/workflows/dotnet.yml) [![NuGet](https://img.shields.io/nuget/v/MBW.Tools.ElephantProject.svg)](https://www.nuget.org/packages/MBW.Tools.ElephantProject) [![GHPackages](https://img.shields.io/badge/package-alpha-green)](https://github.com/LordMike/MBW.Tools.ElephantProject/packages/704608)

Dotnet tool to create an maintain solution files, optionally modifying projects to replace package references with project references. This enables you to perform tasks such as large-scale refactoring or to easier debug tricky bugs.

The tool works in two parts: 

* Modifying package references to project references, enabling cross-repository references. It is not intended for these replacements to be committed to source control.
* Maintain solution files by following some spec. This will both add and remove projects from solution files, allowing you to easily maintain multiple solution files with each their own set of projects. This also ammends the previous point, by enabling you to create cross-repository solution files.

## Installation

Run `dotnet tool install -g MBW.Tools.ElephantProject`. After this, `elephant-project` should be in your PATH.

Run `elephant-project --help` to get further details.

## Usage: Rewrite project files

> elephant-project rewrite -d C:\Root\Directory\

This command replaces `PackageReferences` in `csproj` files with `ProjectReferences`. The goal here is to enable compilation, linking and debugging between two (or more) distinct repositories that usually share code through nuget packages.

## Usage: Create combined solution file

> elephant-project sln -d C:\Root\Directory\ Full.sln

This command creates or modifies a solution file to contain the projects within the directory specified. You can further tweak the projects included using the globbing patterns for `--include` and `--exclude`. This could be to exclude certain projects, like test projects, or to only include some set of projects within the root directory.

Dependent projects are _always_ included in the resulting solution file - even if they are outside the root directory.

# Tips

Use `rewrite` first on the directory containing all your repositories. This replaces all nuget references with project references, between all repositories. Then use `sln` to create a new solution file that encompasses all projects in one go. Opening this solution file should now present you with all your projects in one common place.