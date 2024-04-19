/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
//#define DEV_BUILD
using System;
using System.Diagnostics;
using System.IO;

namespace KarrotSoundProduction;

public static class Utils
{
#if DEV_BUILD
    public const int KSPFormatVersion = 0;
#else
    public const int KSPFormatVersion = 2;
#endif

    public static readonly string cacheDir = $"{Environment.GetEnvironmentVariable("HOME")}/.cache/KarrotSoundProduction";
    public static readonly string configDir = $"{Environment.GetEnvironmentVariable("HOME")}/.config/KarrotSoundProduction";

    public static bool CheckForCommand(string command)
    {
        Process whichProcess = new();
        whichProcess.StartInfo = new("which", command)
        {
            RedirectStandardOutput = true
        };

        whichProcess.Start();
        whichProcess.WaitForExit();
        string whichOutput = whichProcess.StandardOutput.ReadToEnd();

        return whichOutput.StartsWith('/');
    }

    /// <summary>
    /// Audio formats recognized by KSP
    /// </summary>
    public enum AudioFormat
    {
        Wave,
        MP3,
        Flac,
        Unknown
    }

    public static AudioFormat GetFileFormat(string filePath)
    {
        ProcessStartInfo fileCommand = new("file", $"--mime-type \"{filePath.Replace("\"", "\\\"")}\"");
        fileCommand.RedirectStandardOutput = true;
        string fileResults = Process.Start(fileCommand).StandardOutput.ReadToEnd();
        string mimeType = fileResults.Split(':')[^1].Trim();
        return mimeType switch
        {
            "audio/x-wav" or "audio/wav" or "audio/vnd.wav" => AudioFormat.Wave,
            "audio/flac" => AudioFormat.Flac,
            "audio/mpeg" => AudioFormat.MP3,
            _ => AudioFormat.Unknown
        };
    }

    /// <summary>
    /// Retrieves a substring from this instance, or empty if the start index is out of range.
    /// Designed as an exception-free wrapper around Substring.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="startIndex"></param>
    /// <returns></returns>
    public static string SafeSubstring(this string input, int startIndex)
    {
        if (input == null) return "";
        if (startIndex < 0) return "";
        if (startIndex >= input.Length) return "";
        else return input.Substring(startIndex);
    }

    /// <summary>
    /// Retrieves a substring from this instance, empty if the start index is out of range, or up to the end of the string.
    /// Designed as an exception-free wrapper around Substring.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="startIndex"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static string SafeSubstring(this string input, int startIndex, int length)
    {
        if (input == null) return "";
        if (startIndex < 0 || length <= 0) return "";
        if (startIndex >= input.Length) return "";
        else if (startIndex + length > input.Length) return input.Substring(startIndex, input.Length - startIndex);
        else return input.Substring(startIndex, length);
    }

    public static string GetWavePath(string fileName)
    {
        AudioFormat fmt = GetFileFormat(fileName);
        if (fmt == AudioFormat.Flac)
        {
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            Console.WriteLine($"Decoding {fileName}");
            Stopwatch stopwatch = new();
            stopwatch.Start();
            string wavFileName = $"{cacheDir}/{System.IO.Path.GetFileName(System.IO.Path.ChangeExtension(fileName, ".wav"))}";
            if (!File.Exists(wavFileName))
                Process.Start($"flac", $"-fd \"{fileName}\" -o \"{wavFileName}\"").WaitForExit();
            stopwatch.Stop();
            Console.WriteLine($"Decode elapsed time: {stopwatch.ElapsedMilliseconds} ms");
            fileName = wavFileName;
        }
        if (fmt == AudioFormat.MP3)
        {
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            Console.WriteLine($"Decoding {fileName}");
            Stopwatch stopwatch = new();
            stopwatch.Start();
            string wavFileName = $"{cacheDir}/{System.IO.Path.GetFileName(System.IO.Path.ChangeExtension(fileName, ".wav"))}";
            if (!File.Exists(wavFileName))
                Process.Start($"ffmpeg", $"-i \"{fileName.Replace("\"", "\\\"")}\" -acodec pcm_s16le -ar 44100 \"{wavFileName.Replace("\"", "\\\"")}\"").WaitForExit();
            stopwatch.Stop();
            Console.WriteLine($"Decode elapsed time: {stopwatch.ElapsedMilliseconds} ms");
            fileName = wavFileName;
        }
        return fileName;
    }
}
