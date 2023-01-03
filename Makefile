all: pw_bindings
	dotnet publish -c Debug

release: pw_bindings
	dotnet publish -c Release
	cp pw_interface.so bin/Release/net7.0/linux-x64/publish

pub-run: pw_bindings release
	bin/Release/net7.0/linux-x64/publish/KarrotSoundProduction

run: pw_bindings
	dotnet build -r linux-x64 -c Debug
	cp pw_interface.so bin/Debug/net6.0/linux-x64/
	bin/Debug/net7.0/linux-x64/KarrotSoundProduction

pw_bindings:
	clang pipewire_bindings.c -lm -I/usr/include/pipewire-0.3 -I/usr/include/spa-0.2 -D_REENTRANT -lpipewire-0.3 -O3 -s -fPIC -shared -o pw_interface.so -Wall -Werror