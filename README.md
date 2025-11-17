# Aardvark.OpenImageDenoise

[![Build](https://github.com/aardvark-community/Aardvark.OpenImageDenoise/actions/workflows/build.yml/badge.svg)](https://github.com/aardvark-community/Aardvark.OpenImageDenoise/actions/workflows/build.yml)
[![Publish](https://github.com/aardvark-community/Aardvark.OpenImageDenoise/actions/workflows/publish.yml/badge.svg)](https://github.com/aardvark-community/Aardvark.OpenImageDenoise/actions/workflows/publish.yml)
[![Nuget](https://img.shields.io/nuget/vpre/Aardvark.OpenImageDenoise)](https://www.nuget.org/packages/Aardvark.OpenImageDenoise/)
[![Downloads](https://img.shields.io/nuget/dt/Aardvark.OpenImageDenoise)](https://www.nuget.org/packages/Aardvark.OpenImageDenoise/)

Aardvark PixImage bindings for the Intel Open Image Denoise library (Version 1.3.0).

## Build
- Requires Visual Studio 2022 (dotnet tool aardpack)
- Run build.cmd

## Getting Started
See example code in [DenoiseTest](https://github.com/aardvark-community/Aardvark.OpenImageDenoise/tree/master/src/DenoiseTest)

* `Aardvark.Base.Aardvark.Init()` is required to register the native dependencies during runtime. In the example here it is required for PixImage.DevIL and will also register the \lib\Natives directory for the native OpenImageDenoise assemblies. When installing Aardvark.OpenImageDenoise as nuget packages it will register the native assemblies similar to PixImage.DevIL here.
* When using with Intel Embree make sure there is no conflict with the Intel oneAPI Threading Building Blocks assembly (tbb12.dll).
* Have fun coding!
