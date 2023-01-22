# KarrotSoundProduction
KarrotSoundProduction is a free and open source soundboard intended for use in theater productions.
KSP supports WAV, MP3, and FLAC audio.

# Build Instructions
KSP is currently available for Linux on x86_64. To build for another platform, follow these instructions.

1. Ensure that you have the following installed:
   * .NET SDK >= 7.0
   * GTK >= 3.24(may be installed automatically on restore)
   * (Optional) GNU or similar Make utility
   * (Linux) PulseAudio OR Pipewire(set up to play audio) OR ALSA, mpg123
   * (Linux/Mac OSX) FLAC decoder available on path as `flac`
   * (Linux) FFMPEG available on path as `ffmpeg`
   * (Linux) C compiler(gcc or clang)
2. Clone the repository locally.
3. Navigate into the repository's root directory.
4. `dotnet restore`
5. Run either `make` or an individual `dotnet publish` command. Note that if you do not use make, you may need to [specify a runtime](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish#options).
   * To build in release configuration rather than the default of debug, run `make release` or add `-c Release` to the `dotnet publish` command.
   * Linux: If you do not have clang installed, modify the Makefile and replace `clang` with `gcc`.
6. Navigate to `bin/Debug/net7.0/<runtime>/KarrotSoundProduction/publish` and run the executable found there.


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
