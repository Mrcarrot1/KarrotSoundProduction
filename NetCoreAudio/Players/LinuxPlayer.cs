using NetCoreAudio.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NetCoreAudio.Players
{
    internal class LinuxPlayer : UnixPlayerBase, IPlayer
    {
        protected override string GetBashCommand(string fileName)
        {
            if (Path.GetExtension(fileName).ToLower().Equals(".mp3"))
            {
                return "mpg123 -q";
            }
            else
            {
                return "paplay";
            }
        }

        public override Task SetVolume(int percent)
        {
            if (percent > 100) percent = 100;

            var tempProcess = StartBashProcess($"amixer -M set 'Master' {percent}%");
            tempProcess.WaitForExit();

            return Task.CompletedTask;
        }
    }
}
