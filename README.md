# MFF Totem

## Prerequisities
 * MonoGame 3.6+
 
## Used Fonts
 * [Sansation Regular](https://www.dafont.com/sansation.font)

## How to Build
 * Clone the repository
 * Fetch the submodules with `git submodule update --init --recursive`
 * Install [MonoGame 3.6](http://www.monogame.net/downloads/)
 * If you're on Windows, add `Program Files\MSBuild\MonoGame\v3.0\Tools\MGCB.exe` to the `%path%`
 * Fetch libraries with either `getlibs.sh` or `getlibs.ps1`
 * Move the content of `libs/%yourplatform%` to `Mff.Totem/bin/DesktopGL/AnyCPU/Debug`
   * (Is there any relevant reason for not moving the files automatically with the scripts?)
 * Use the `make` command on Unix and `make.bat` on Windows to:
   * `make content`
   * `make build`
   * `make run`
