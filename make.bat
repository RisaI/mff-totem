@echo off

if "%1" == "build" (

    :: Find MSBuild
    FOR /F "usebackq tokens=2,* skip=2" %%L IN (
       `reg query "HKLM\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v MSBuildToolsPath`
    ) DO SET msbuild="%%MMSBuild.exe"

    echo Building...
    echo MSBuild path:
    echo %msbuild%

    if "%2" == "release" (
        %msbuild% /p:Configuration=Release /p:Platform="Any CPU"
    ) else (
        %msbuild% /p:Configuration=Debug /p:Platform="Any CPU"
    )
)

if "%1" == "content" (

    cd Content
    mgcb /@:Content.mgcb
    .\shaders.bat
    cd ..
    mkdir Mff.Totembin\DesktopGL\AnyCPU\Debug\Content
    if "%2" == "release" (
        robocopy Content\bin\DesktopGL Mff.Totem\bin\DesktopGL\AnyCPU\Release\Content\ /SEC /E
    ) else (
        robocopy Content\bin\DesktopGL Mff.Totem\bin\DesktopGL\AnyCPU\Debug\Content\ /SEC /E
    )

)

if "%1" == "content-ns" (

    cd Content
    mgcb /@:Content.mgcb
    cd ..
    mkdir Mff.Totembin\DesktopGL\AnyCPU\Debug\Content
    if "%2" == "release" (
        robocopy Content\bin\DesktopGL Mff.Totem\bin\DesktopGL\AnyCPU\Release\Content\ /SEC /E
    ) else (
        robocopy Content\bin\DesktopGL Mff.Totem\bin\DesktopGL\AnyCPU\Debug\Content\ /SEC /E
    )

)

if "%1" == "libs" (
    powershell ./getlibs.ps1
    echo If this command failed, extract the contents of the appropriate subfolder of libs.tar to Mff.Totem\bin\DesktopGL\AnyCPU\Debug\
)

if "%1" == "run" (
    pushd "%~dp0\Mff.Totem\bin\DesktopGL\AnyCPU\"

    if "%2" == "release" (
        cd Release
    ) else (
        cd Debug
    )

    Mff.Totem.exe

    popd
)

if "%1" == "clean" (
    del /S /Q Mff.Totem\bin\*
    del /S /Q Content\bin\*
    del /S /Q libs
    del /Q libs.tar.bz2
    del /Q libs.tar
)