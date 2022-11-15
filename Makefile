all:
	clang pipewire_bindings.c -lm -I/usr/include/pipewire-0.3 -I/usr/include/spa-0.2 -D_REENTRANT -lpipewire-0.3 -O3 -s -fPIC -shared -o pw_interface.so
	dotnet publish -r linux-x64 -c Debug
	dotnet publish -r linux-arm -c Debug
	dotnet publish -r win-x64 -c Debug
	dotnet publish -r win-arm -c Debug
	dotnet publish -r osx-x64 -c Debug
release:
	clang pipewire_bindings.c -lm -I/usr/include/pipewire-0.3 -I/usr/include/spa-0.2 -D_REENTRANT -lpipewire-0.3 -O3 -s -fPIC -shared -o pw_interface.so
	dotnet publish -r linux-x64 -c Release
	dotnet publish -r linux-arm -c Release
	dotnet publish -r win-x64 -c Release
	dotnet publish -r win-arm -c Release
	dotnet publish -r osx-x64 -c Release
run:
	clang pipewire_bindings.c -lm -I/usr/include/pipewire-0.3 -I/usr/include/spa-0.2 -D_REENTRANT -lpipewire-0.3 -O3 -s -fPIC -shared -o pw_interface.so
	dotnet build -r linux-x64 -c Debug
	cp pw_interface.so bin/Debug/net7.0/linux-x64/
	bin/Debug/net7.0/linux-x64/KarrotSoundProduction