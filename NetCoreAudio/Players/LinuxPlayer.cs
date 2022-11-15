using NetCoreAudio.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NetCoreAudio.Players
{
    internal class LinuxPlayer : UnixPlayerBase, IPlayer
    {
        public enum PlayerBackend
        {
            ALSA,
            PulseAudio,
            Pipewire,
            NativePipewire
        }

        public PlayerBackend Backend { get; set; }

        protected override string GetBashCommand(string fileName)
        {
            if (Path.GetExtension(fileName).ToLower().Equals(".mp3"))
            {
                return $"mpg123 -q -f {CurrentVolume * 32768 / 100}";
            }
            else
            {
                return Backend switch
                {
                    PlayerBackend.PulseAudio => $"paplay --volume={CurrentVolume * 65536 / 100}",
                    PlayerBackend.Pipewire => $"pw-play --volume {(float)CurrentVolume / 100}",
                    PlayerBackend.ALSA => $"",
                    _ => "echo '[KSP] Internal error: Unknown backend- could not play file'"
                };
            }
        }

        public override Task SetVolume(int percent)
        {
            if (percent > 100) percent = 100;

            CurrentVolume = percent;

            var tempProcess = StartBashProcess($"amixer -M set 'Master' {percent}%");
            tempProcess.WaitForExit();

            return Task.CompletedTask;
        }
    }
}
