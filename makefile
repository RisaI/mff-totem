build:
	mdtool build Mff.Totem.sln

run:
	cd Mff.Totem/bin/DesktopGL/AnyCPU/Debug/; chmod +x Mff.Totem.exe; ./Mff.Totem.exe

clean:
	rm -r Mff.Totem/bin/
