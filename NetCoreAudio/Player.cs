using NetCoreAudio.Interfaces;
using NetCoreAudio.Players;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NetCoreAudio
{
    public class Player : IPlayer
    {
        public int CurrentVolume
        {
            get
            {
                return _internalPlayer.CurrentVolume;
            }
            set
            {
                _internalPlayer.CurrentVolume = value;
            }
        }

        private readonly IPlayer _internalPlayer;

        /// <summary>
        /// Internally, sets Playing flag to false. Additional handlers can be attached to it to handle any custom logic.
        /// </summary>
        public event EventHandler PlaybackFinished;

        /// <summary>
        /// Indicates that the audio is currently playing.
        /// </summary>
        public bool Playing => _internalPlayer.Playing;

        /// <summary>
        /// Indicates that the audio playback is currently paused.
        /// </summary>
        public bool Paused => _internalPlayer.Paused;

        public Player(bool useNAudio = true)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (useNAudio)
                    _internalPlayer = new WindowsPlayerNAudio();
                else
                    _internalPlayer = new WindowsPlayer();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (KarrotSoundProduction.Utils.CheckForCommand("pw-play"))
                {
                    _internalPlayer = new LinuxPlayerNative();
                }
                else
                {
                    LinuxPlayer internalPlayer = new LinuxPlayer();

                    if (KarrotSoundProduction.Utils.CheckForCommand("paplay"))
                        internalPlayer.Backend = LinuxPlayer.PlayerBackend.PulseAudio;
                    else if (KarrotSoundProduction.Utils.CheckForCommand("aplay"))
                        internalPlayer.Backend = LinuxPlayer.PlayerBackend.ALSA;
                    else
                        throw new Exception("Missing dependency: Pipewire, PulseAudio, or ALSA backend");

                    if (!KarrotSoundProduction.Utils.CheckForCommand("mpg123"))
                        throw new Exception("Missing dependency: mpg123");

                    if (!KarrotSoundProduction.Utils.CheckForCommand("flac"))
                        throw new Exception("Missing dependency: flac");

                    _internalPlayer = internalPlayer;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                _internalPlayer = new MacPlayer();
            else
                throw new Exception("No NetCoreAudio implementation exists for the current OS!");

            _internalPlayer.CurrentVolume = 100;

            _internalPlayer.PlaybackFinished += OnPlaybackFinished;
        }

        /// <summary>
        /// Will stop any current playback and will start playing the specified audio file. The fileName parameter can be an absolute path or a path relative to the directory where the library is located. Sets Playing flag to true. Sets Paused flag to false.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task Play(string fileName)
        {
            await _internalPlayer.Play(fileName);
        }

        public async Task Play(string fileName, KarrotSoundProduction.SoundConfiguration config)
        {
            if (_internalPlayer is LinuxPlayerNative lpn)
            {
                lpn.Play(fileName, config);
            }
            else throw new NotImplementedException();
        }

        public async Task Play(string fileName, int fadeInMilliseconds, int fadeOutMilliseconds)
        {
            if (_internalPlayer is not LinuxPlayerNative lpn && fadeInMilliseconds != 0 && fadeOutMilliseconds != 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.WriteLine($"Fade in/out time is not supported on the {GetPlayerBackend()} backend");
                Console.ResetColor();
                return;
            }

            
        }

        /// <summary>
        /// Pauses any ongong playback. Sets Paused flag to true. Doesn't modify Playing flag.
        /// </summary>
        /// <returns></returns>
        public async Task Pause()
        {
            await _internalPlayer.Pause();
        }

        /// <summary>
        /// Resumes any paused playback. Sets Paused flag to false. Doesn't modify Playing flag.
        /// </summary>
        /// <returns></returns>
        public async Task Resume()
        {
            await _internalPlayer.Resume();
        }

        /// <summary>
        /// Stops any current playback and clears the buffer. Sets Playing and Paused flags to false.
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            await _internalPlayer.Stop();
        }

        private void OnPlaybackFinished(object sender, EventArgs e)
        {
            PlaybackFinished?.Invoke(this, e);
        }

        /// <summary>
        /// Sets the playing volume as percent
        /// </summary>
        /// <returns></returns>
        public async Task SetVolume(int percent)
        {
            CurrentVolume = percent;
            await _internalPlayer.SetVolume(percent);
        }

        public async Task SetVolume(double log2Scale)
        {
            int percent = (int)Math.Pow(2, Math.Log10(log2Scale));
            CurrentVolume = percent;
            await _internalPlayer.SetVolume(log2Scale);
        }

        public string GetPlayerBackend()
        {
            return _internalPlayer switch
            {
                LinuxPlayer player => $"Linux({player.Backend})",
                LinuxPlayerNative => "Linux(Native Pipewire)",
                MacPlayer => $"MacOS",
                WindowsPlayer => $"Windows(NetCoreAudio)",
                WindowsPlayerNAudio => $"Windows(NAudio)",
                _ => "Unknown backend"
            };
        }
    }
}
