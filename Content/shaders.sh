#!/bin/bash
#

mkdir -p bin/DesktopGL/shaders
for f in ./shaders/*.fx; do
echo $f	
wine $FXC_PATH /T fx_2_0 /Fo bin/DesktopGL/shaders/$(basename $f .fx).xnb $f
done
