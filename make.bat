@echo off

if "%1" == "build" (

    :: Find MSBuild
    FOR /F "usebackq tokens=2,* skip=2" %%L IN (
       `reg query "HKLM\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v MSBuildToolsPath`
    ) DO SET msbuild="%%MMSBuild.exe"

    echo %msbuild%

    %msbuild%
)