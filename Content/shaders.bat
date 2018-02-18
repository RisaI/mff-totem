@echo off

mkdir bin\DesktopGL\shaders
for /R %%f in (.\shaders\*.fx) do (
    echo %%f
    fxc /T fx_2_0 /Fo "bin\DesktopGL\shaders\%%~nf.xnb" "%%f"
)
