#!/bin/bash
#

curl http://fna.flibitijibibo.com/archive/fnalibs.tar.bz2 > libs.tar.bz2
mkdir libs
tar xf libs.tar.bz2 -C libs
rm libs.tar.bz2
echo Libs have been extracted to the libs/ directory. Copy files for your platform to the debug folder.