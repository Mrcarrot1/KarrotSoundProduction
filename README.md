# KarrotSoundProduction
KarrotSoundProduction is a free and open source soundboard intended for use in theater productions.
It (will have) support for playing multiple sounds at once as well as indefinitely long sound files.
Supported media types (will) include FLAC, WAV, MP3, AAC, and WMA.

This software was designed as a replacement for the now-defunct EXP Soundboard.
As such, it has full backwards compatibility with EXP's configuration files, without the reliance on outdated software.

# Build Instructions
KSP is not currently available in binary form. If you would like to run the very early test versions currently available, follow these instructions.

1. Ensure that you have the following installed:
   * .NET SDK >= 6.0
   * GTK >= 3.24(may be installed automatically on restore)
   * (Optional) GNU or similar Make utility
   * (Linux) PulseAudio OR Pipewire(set up to play audio) OR ALSA, mpg123
   * (Linux/Mac OSX) FLAC decoder available on path as `flac`
2. Clone the repository locally.
3. Navigate into the repository's root directory.
4. `dotnet restore`
5. Run either `make` or an individual `dotnet publish` command. Note that if you do not use make, you may need to [specify a runtime](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish#options).
   * To build in release configuration rather than the default of debug, run `make release` or add `-c Release` to the `dotnet publish` command.
6. Navigate to `bin/Debug/net6.0/<runtime>/KarrotSoundProduction/publish` and run the executable found there.


# License
KSP as a complete product is licensed under the Mozilla Public License, version 2.0. For more details, see LICENSE in this directory.

KSP uses a modified version of NetCoreAudio, originally published on GitHub by mobiletechtracker.
The original project's license is as follows.

```
MIT License

Copyright (c) 2020 mobiletechtracker

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

KSP also uses NAudio to play audio on Windows.
That project's license is as follows.

```
Copyright 2020 Mark Heath

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```