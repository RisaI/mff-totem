build:
	msbuild Mff.Totem.sln

content:
	cd Content; mgcb Content.mgcb; ./shaders.sh; cd ..; mkdir -p Mff.Totem/bin/DesktopGL/AnyCPU/Debug/Content; cp -ru Content/bin/DesktopGL/* Mff.Totem/bin/DesktopGL/AnyCPU/Debug/Content/

content-ns:
	cd Content; mgcb Content.mgcb; cd ..; mkdir -p Mff.Totem/bin/DesktopGL/AnyCPU/Debug/Content; cp -ru Content/bin/DesktopGL/* Mff.Totem/bin/DesktopGL/AnyCPU/Debug/Content/

run:
	cd Mff.Totem/bin/DesktopGL/AnyCPU/Debug/; chmod +x Mff.Totem.exe; mono ./Mff.Totem.exe

clean:
	rm -r Mff.Totem/bin/
