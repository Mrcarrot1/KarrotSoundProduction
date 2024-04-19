CFLAGS=-I/usr/include/pipewire-0.3 -I/usr/include/spa-0.2 -D_REENTRANT -fPIC -O3 -Wall -Werror

all: pw_bindings
	dotnet publish -c Debug

release: pw_bindings
	dotnet publish -c Release
	cp pw_interface.so bin/Release/net8.0/linux-x64/publish

pub-run: pw_bindings release
	bin/Release/net8.0/linux-x64/publish/KarrotSoundProduction

run: pw_bindings
	dotnet build -r linux-x64 -c Debug
	cp pw_interface.so bin/Debug/net8.0/linux-x64/
	bin/Debug/net8.0/linux-x64/KarrotSoundProduction

pw_bindings: player_main player_funcs process_funcs
	clang pipewire_bindings/ksp_pw_player_main.o pipewire_bindings/ksp_pw_player_funcs.o pipewire_bindings/ksp_pw_process_funcs.o -lm -lpipewire-0.3 -s -fPIC -shared -o pw_interface.so -Wall -Werror

standalone_player: standalone_player_main player_main player_funcs process_funcs
	clang pipewire_bindings/ksp_pw_player_main.o pipewire_bindings/ksp_pw_player_funcs.o pipewire_bindings/ksp_pw_process_funcs.o pipewire_bindings/standalone_player_main.o -lm -lpipewire-0.3 -ggdb -o pipewire_bindings/standalone_player -Wall -Werror

standalone_player_main:
	clang pipewire_bindings/standalone_player_main.c -c $(CFLAGS) -ggdb -o pipewire_bindings/standalone_player_main.o

player_main:
	clang pipewire_bindings/ksp_pw_player_main.c -c $(CFLAGS) -ggdb -o pipewire_bindings/ksp_pw_player_main.o

player_funcs:
	clang pipewire_bindings/ksp_pw_player_funcs.c -c $(CFLAGS) -ggdb -o pipewire_bindings/ksp_pw_player_funcs.o

process_funcs:
	clang pipewire_bindings/ksp_pw_process_funcs.c -c $(CFLAGS) -ggdb -o pipewire_bindings/ksp_pw_process_funcs.o

