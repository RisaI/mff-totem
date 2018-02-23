#!/bin/bash
#

curl http://fna.flibitijibibo.com/archive/fnalibs.tar.bz2 > libs.tar.bz2
mkdir libs
tar xf libs.tar.bz2 -C libs
rm libs.tar.bz2

# Copy relevant libraries, if OS recognized
mkdir -p Mff.Totem/bin/DesktopGL/AnyCPU/Debug/
if [[ $OSTYPE == "linux-gnu" ]]; then
	if [[ $(uname -m) == "x86_64" ]]; then
		cp libs/lib64/* Mff.Totem/bin/DesktopGL/AnyCPU/Debug/
	else
		cp libs/lib/* Mff.Totem/bin/DesktopGL/AnyCPU/Debug/
	fi
fi
