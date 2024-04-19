using NetCoreAudio.Interfaces;
using System;
using System.Threading.Tasks;

namespace NetCoreAudio.Players
{
    internal class MacPlayer : UnixPlayerBase, IPlayer
    {
        protected override string GetBashCommand(string fileName)
        {
            return "afplay";
        }

        public override Task SetVolume(double log2Scale) => SetVolume((int)Math.Pow(2, Math.Log10(log2Scale)));

        public override Task SetVolume(int percent)
        {
            if (percent > 100) percent = 100;

            var tempProcess = StartBashProcess($"osascript -e \"set volume output volume {percent}\"");
            tempProcess.WaitForExit();

            return Task.CompletedTask;
        }
    }
}
