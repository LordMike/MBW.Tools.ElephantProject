# MBW.Tools.ElephantProject [![Generic Build](https://github.com/LordMike/MBW.Tools.ElephantProject/actions/workflows/dotnet.yml/badge.svg)](https://github.com/LordMike/MBW.Tools.ElephantProject/actions/workflows/dotnet.yml) [![NuGet](https://img.shields.io/nuget/v/MBW.Tools.ElephantProject.svg)](https://www.nuget.org/packages/MBW.Tools.ElephantProject) [![GHPackages](https://img.shields.io/badge/package-alpha-green)](https://github.com/LordMike/MBW.Tools.ElephantProject/packages/703133)

Dotnet tool to create an maintain solution files, optionally modifying projects to replace package references with project references. This enables you to perform tasks such as large-scale refactoring or to easier debug tricky bugs.

The tool works in two parts: 

* Modifying package references to project references, enabling cross-repository references. It is not intended for these replacements to be committed to source control.
* Maintain solution files by following some spec. This will both add and remove projects from solution files, allowing you to easily maintain multiple solution files with each their own set of projects. This also ammends the previous point, by enabling you to create cross-repository solution files.

## Installation

Run `dotnet tool install -g MBW.Tools.ElephantProject`. After this, `elephant-project` should be in your PATH.

## Usage

TODO