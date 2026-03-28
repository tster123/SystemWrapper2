# SystemWrapper2

[![NuGet version](https://img.shields.io/nuget/v/SystemWrapper2.svg)](https://www.nuget.org/packages/SystemWrapper2/)

**SystemWrapper2** is .NET library for easier testing of system APIs.

This project is intended as a successor of the excellent SystemWrapper project.
The system APIs in .NET are poorly designed to make it hard to do dependency injection and mocking.
This project creates an interface for them and a wrapping class that implements the interface.
It also does this for static classes like Path, File, and Directory.

## Motivation

Maintenance of the original SystemWrapper is expensive since it requires someone to update the code for each framework version.
This package instead uses code to generate the wrappers for each framework version so the only thing that needs to be modified is a list of versions.

## Usage

To install SystemWrapper2, run the following commands in the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console)


```
Install-Package SystemWrapper2
```
