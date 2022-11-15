using System;
using System.Threading.Tasks;

namespace NetCoreAudio.Interfaces
{
    public interface IPlayer
    {
        event EventHandler PlaybackFinished;

        int CurrentVolume { get; set; }

        bool Playing { get; }
        bool Paused { get; }

        Task Play(string fileName);
        Task Pause();
        Task Resume();
        Task Stop();
        Task SetVolume(int percent);
    }
}
