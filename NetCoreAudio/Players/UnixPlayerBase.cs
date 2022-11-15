using NetCoreAudio.Interfaces;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace NetCoreAudio.Players
{
    internal abstract class UnixPlayerBase : IPlayer
    {
        public int CurrentVolume { get; set; }

        private Process _process = null;

        internal const string PauseProcessCommand = "kill -STOP {0}";
        internal const string ResumeProcessCommand = "kill -CONT {0}";

        public event EventHandler PlaybackFinished;

        public bool Playing { get; private set; }

        public bool Paused { get; private set; }

        protected abstract string GetBashCommand(string fileName);

        public async Task Play(string fileName)
        {
            await Stop();
            if (Path.GetExtension(fileName).ToLower() == ".flac")
            {
                if (!Directory.Exists("temp"))
                {
                    Directory.CreateDirectory("temp");
                }
                Console.WriteLine($"Decoding {fileName}");
                string wavFileName = $"temp/{Path.GetFileName(Path.ChangeExtension(fileName, ".wav"))}";
                if (!File.Exists(wavFileName))
                    Process.Start($"\flac", $"-fd '{fileName}' -o '{wavFileName}'").WaitForExit();
                fileName = wavFileName;
            }
            Console.WriteLine($"Playing {fileName}");
            var BashToolName = GetBashCommand(fileName);
            _process = StartBashProcess($"{BashToolName} {EscapeFileName(fileName)}");
            _process.EnableRaisingEvents = true;
            _process.Exited += HandlePlaybackFinished;
            _process.ErrorDataReceived += HandlePlaybackFinished;
            _process.Disposed += HandlePlaybackFinished;
            Playing = true;
        }

        public Task Pause()
        {
            if (Playing && !Paused && _process != null)
            {
                var tempProcess = StartBashProcess(string.Format(PauseProcessCommand, _process.Id));
                tempProcess.WaitForExit();
                Paused = true;
            }

            return Task.CompletedTask;
        }

        public Task Resume()
        {
            if (Playing && Paused && _process != null)
            {
                var tempProcess = StartBashProcess(string.Format(ResumeProcessCommand, _process.Id));
                tempProcess.WaitForExit();
                Paused = false;
            }

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (_process != null)
            {
                _process.Kill();
                _process.Dispose();
                _process = null;
            }

            Playing = false;
            Paused = false;

            return Task.CompletedTask;
        }

        protected Process StartBashProcess(string command)
        {
            var escapedArgs = command.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            return process;
        }

        internal void HandlePlaybackFinished(object sender, EventArgs e)
        {
            if (Playing)
            {
                Playing = false;
                PlaybackFinished?.Invoke(this, e);
            }
        }

        public abstract Task SetVolume(int percent);

        private static string EscapeFileName(string fileName)
        {
            return fileName
                .Replace(" ", "\\ ")
                .Replace("\"", "\\\"")
                .Replace("'", "\\'");
        }
    }
}
