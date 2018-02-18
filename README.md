# MFF Totem

## Prerequisites
 * MonoGame 3.6+
 * DirectX SDK to build the shaders
 
## Used Fonts
 * [Sansation Regular](https://www.dafont.com/sansation.font)
 * [Ubuntu Mono](http://www.monogame.net/downloads/)

## How to Build
1. Clone the repository
1. Fetch the submodules with `git submodule update --init --recursive`
1. Install [MonoGame 3.6](http://www.monogame.net/downloads/)
   * **[Windows]** Add `Program Files\MSBuild\MonoGame\v3.0\Tools\MGCB.exe` to the `%path%`
1. Install DirectX SDK or Windows SDK
   * @RisaI Could you provide more info about making it work on Linux?
   * **[Windows]** Add `fxc.exe` to `%path%`. It is typically found somewhere like `Program Files\Windows Kits\10\bin\10.0.16299.0\x64`
   
1. **[Windows]** Install [C++2012 redist](https://www.microsoft.com/en-us/download/details.aspx?id=30679), both x86 and x64
1. Install the fonts under [Used Fonts](#used-fonts)
1. Fetch libraries with either `getlibs.sh` or `getlibs.ps1`
1. Move the content of `libs/%yourplatform%` to `Mff.Totem/bin/DesktopGL/AnyCPU/Debug`
   * (Is there any relevant reason for not moving the files automatically with the scripts?)
1. Use the `make` command on Unix and `make.bat` on Windows to:
   * `make content`
   * `make build`
   * `make run`
