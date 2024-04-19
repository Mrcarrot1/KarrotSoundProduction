/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using Gdk;
using Gio;
using NetCoreAudio.Interfaces;

namespace NetCoreAudio.Players;

internal partial class LinuxPlayerNative : IPlayer
{
    public event EventHandler PlaybackFinished;

    public int CurrentVolume { get; set; }

    public bool Playing { get; private set; }
    public bool Paused { get; }

    public LinuxPlayer.PlayerBackend Backend { get { return LinuxPlayer.PlayerBackend.NativePipewire; } }

    private static partial class Interop
    {
        [LibraryImport("pw_interface.so")]
        public static unsafe partial void startPlayer(pw_player_info* info, int argc, byte** argv);

        [LibraryImport("pw_interface.so")]
        public static unsafe partial void stopPlayer(void* userdata, int signal);

        [LibraryImport("pw_interface.so")]
        public static unsafe partial void setVolume(pw_player_info* info, float volume);

        [LibraryImport("pw_interface.so")]
        public static unsafe partial float getVolume(pw_player_info* info);

        [LibraryImport("pw_interface.so")]
        public static unsafe partial void setPaused(pw_player_info* info, [MarshalAs(UnmanagedType.Bool)] bool paused);

        //Import C strlen for string operations
        [LibraryImport("libc")]
        public static unsafe partial IntPtr strlen(byte* contents);

        public static unsafe string ConvertFromCStr(byte* cstr)
        {
            if (cstr == null) throw new NullReferenceException("Received null pointer as input.");
            byte[] bytes = CreateArray<byte>(cstr, (int)strlen(cstr));
            string output = Encoding.ASCII.GetString(bytes);
            return output;
        }

        public static unsafe byte* ConvertToCStr(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            fixed (byte* ptr = bytes)
            {
                return ptr;
            }
        }

        public unsafe static T[] CreateArray<T>(void* source, int length) where T : struct
        {
            var type = typeof(T);
            var sizeInBytes = Marshal.SizeOf(typeof(T));

            T[] output = new T[length];

            if (type.IsPrimitive)
            {
                // Make sure the array won't be moved around by the GC 
                var handle = GCHandle.Alloc(output, GCHandleType.Pinned);

                var destination = (byte*)handle.AddrOfPinnedObject().ToPointer();
                var byteLength = length * sizeInBytes;

                // There are faster ways to do this, particularly by using wider types or by 
                // handling special lengths.
                for (int i = 0; i < byteLength; i++)
                    destination[i] = ((byte*)source)[i];

                handle.Free();
            }
            else if (type.IsValueType)
            {
                if (!type.IsLayoutSequential && !type.IsExplicitLayout)
                {
                    throw new InvalidOperationException(string.Format("{0} does not define a StructLayout attribute", type));
                }

                IntPtr sourcePtr = new(source);

                for (int i = 0; i < length; i++)
                {
                    IntPtr p = new((byte*)source + i * sizeInBytes);

                    output[i] = (T)Marshal.PtrToStructure(p, typeof(T))!;
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("{0} is not supported", type));
            }

            return output;
        }
    }

    private unsafe pw_player_info playerInfo;

    public Task Play(string fileName)
    {
        return Play(fileName, new KarrotSoundProduction.SoundConfiguration(fileName, Gdk.Key.A));
    }

    public Task Play(string fileName, KarrotSoundProduction.SoundConfiguration config)
    {
        Console.WriteLine($"Playing {fileName} with Native Pipewire backend");
        string[] args = Environment.GetCommandLineArgs();
        int argc = args.Length;
        playerInfo.firstRun = true;
        unsafe
        {
            playerInfo.volume = (float)Math.Pow(10, Math.Log2((double)CurrentVolume / 100));
            playerInfo.fileName = Interop.ConvertToCStr(fileName);
            playerInfo.playing = true;
            playerInfo.speedFactor = config.PlaybackSpeed;
            playerInfo.fadeInMilliseconds = config.FadeInTime;
            playerInfo.fadeOutMilliseconds = config.FadeOutTime;

            byte*[] charPtrs = new byte*[argc];
            for (int i = 0; i < argc; i++)
            {
                charPtrs[i] = Interop.ConvertToCStr(args[i]);
            }
            fixed (byte** argv = charPtrs)
            fixed (pw_player_info* player_info = &playerInfo)
            {
                Console.WriteLine("Starting native player");
                try
                {
                    Playing = true;
                    Interop.startPlayer(player_info, argc, argv);
                }
                catch (Exception e) { Console.WriteLine(e.ToString()); }
            }
        }

        PlaybackFinished.Invoke(this, new EventArgs());
        Playing = false;

        return Task.CompletedTask;
    }

    public Task Pause()
    {
        unsafe
        {
            playerInfo.playing = false;
            fixed (pw_player_info* player_info = &playerInfo)
            Interop.setPaused(player_info, true);
        }

        return Task.CompletedTask;
    }

    public Task Resume()
    {
        unsafe
        {
            playerInfo.playing = true;
            fixed (pw_player_info* player_info = &playerInfo)
            Interop.setPaused(player_info, false);
        }

        return Task.CompletedTask;
    }

    public Task Stop()
    {
        if (!Playing) return Task.CompletedTask;
        unsafe
        {
            fixed (pw_player_info* player_info = &playerInfo)
            {
                Interop.stopPlayer(player_info, 0);
            }
        }
        Playing = false;
        PlaybackFinished.Invoke(this, new());
        return Task.CompletedTask;
    }

    public Task SetVolume(int percent)
    {
        CurrentVolume = percent;
        unsafe
        {
            fixed (pw_player_info* player_info = &playerInfo)
            {
                //Console.WriteLine($"Current volume: {Interop.getVolume(player_info)}");
                //Convert the percent into 0-1 log scale by doing the following:
                //1) Cast to double and divide by 100
                //2) Take the log base 2 of that value
                //3) Take 10 to that power
                double value = Math.Pow(10, Math.Log2((double)percent / 100));
                playerInfo.volume = (float)value;
                Interop.setVolume(player_info, (float)value);
            }
        }

        return Task.CompletedTask;
    }

    public Task SetVolume(double log2Scale)
    {
        CurrentVolume = (int)Math.Pow(2, Math.Log10(log2Scale));
        unsafe
        {
            fixed (pw_player_info* player_info = &playerInfo)
            {
                playerInfo.volume = (float)log2Scale;
                Interop.setVolume(player_info, (float)log2Scale);
            }
        }
        return Task.CompletedTask;
    }

    public Task SetFadeInTime(float seconds)
    {
        playerInfo.fadeInMilliseconds = (int)(seconds / 1000);
        return Task.CompletedTask;
    }

    public Task SetFadeOutTime(float seconds)
    {
        playerInfo.fadeOutMilliseconds = (int)(seconds / 1000);
        return Task.CompletedTask;
    }

    public LinuxPlayerNative()
    {
        unsafe
        {
            playerInfo = new pw_player_info();
        }
        CurrentVolume = 100;
    }
}

unsafe struct pw_player_info
{
    public float volume;
    public byte* fileName;
    public bool playing;
    public bool firstRun;
    KarrotSoundProduction.Utils.AudioFormat format;
    public int fadeInMilliseconds;
    public int fadeOutMilliseconds;
    public float speedFactor;
    public void* data;
}

unsafe struct waveFile
{
    fixed byte chunkId[5]; //Should always be RIFF
    uint chunkSize;
    fixed byte format[5]; //Should always be WAVE
    waveFormatSubChunk formatChunk;
    waveDataSubChunk dataChunk;
}

struct waveFormatSubChunk
{
    ushort audioFormat;
    ushort channels;
    uint sampleRate;
    uint avgBytesPerSec;
    ushort blockAlign; //(Bits per sample * channels) / 8
    ushort bitsPerSample;
}

unsafe struct waveDataSubChunk
{
    uint dataSize;
    byte* data;
}
