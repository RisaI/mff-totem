@echo off

if "%1" == "build" (

    :: Find MSBuild
    FOR /F "usebackq tokens=2,* skip=2" %%L IN (
       `reg query "HKLM\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v MSBuildToolsPath`
    ) DO SET msbuild="%%MMSBuild.exe"

    echo %msbuild%

    %msbuild%
)

if "%1" == "content" (

    cd Content;
    mgcb Content.mgcb
    .\shaders.sh
    cd ..
    mkdir Mff.Totembin\DesktopGL\AnyCPU\Debug\Content
    copy /Y Content\bin\DesktopGL\* Mff.Totem\bin\DesktopGL\AnyCPU\Debug\Content\

)

if "%1" == "content-ns" (

    cd Content;
    mgcb /build:Content.mgcb
    cd ..
    mkdir Mff.Totembin\DesktopGL\AnyCPU\Debug\Content
    copy /Y Content\bin\DesktopGL\* Mff.Totem\bin\DesktopGL\AnyCPU\Debug\Content\

)

if "%1" = "run" (
    Mff.Totem/bin/DesktopGL/AnyCPU/Debug//Mff.Totem.exe
)

if "%1" = "clean" (
    del /Q Mff.Totem/bin/*
)