all:
	dotnet publish -r linux-x64 -c Debug
	dotnet publish -r linux-arm -c Debug
	dotnet publish -r win-x64 -c Debug
	dotnet publish -r win-arm -c Debug
	dotnet publish -r osx-x64 -c Debug
release:
	dotnet publish -r linux-x64 -c Release
	dotnet publish -r linux-arm -c Release
	dotnet publish -r win-x64 -c Release
	dotnet publish -r win-arm -c Release
	dotnet publish -r osx-x64 -c Release
