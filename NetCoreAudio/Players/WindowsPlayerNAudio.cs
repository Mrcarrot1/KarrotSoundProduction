/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using NetCoreAudio.Interfaces;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using NetCoreAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Codecs;
using NAudio.FileFormats;


namespace NetCoreAudio.Players
{
    internal class WindowsPlayerNAudio : IPlayer
    {
        public int CurrentVolume { get; set; }

        private WaveOutEvent waveOut;

        private Timer _playbackTimer;
        private Stopwatch _playStopwatch;

        private string _fileName;

        public event EventHandler PlaybackFinished;

        public bool Playing { get; private set; }
        public bool Paused { get; private set; }

        public Task Play(string fileName)
        {
            FileUtil.ClearTempFiles();
            _fileName = $"\"{FileUtil.CheckFileToPlay(fileName)}\"";
            BufferedWaveProvider waveProvider = null;
            if (Path.GetExtension(fileName) == ".mp3")
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(File.Open(fileName, FileMode.Open));
                IMp3FrameDecompressor decompressor = new AcmMp3FrameDecompressor(new Mp3WaveFormat(frame.BitRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate));
                waveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                waveProvider.BufferDuration = TimeSpan.FromSeconds(20);
                byte[] buffer = new byte[65536];
                int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                waveProvider.AddSamples(buffer, 0, decompressed);
            }
            if (Path.GetExtension(fileName) == ".wav")
            {
                WaveFileReader fileReader = new(fileName);
                waveProvider = new BufferedWaveProvider(fileReader.WaveFormat);
                waveProvider.BufferDuration = TimeSpan.FromSeconds(20);
            }
            if (Path.GetExtension(fileName) == ".flac")
            {
                //waveProvider = new 
            }

            waveOut.Init(waveProvider);

            _playbackTimer = new Timer
            {
                AutoReset = false
            };
            _playStopwatch = new Stopwatch();

            Paused = false;
            Playing = true;
            _playbackTimer.Elapsed += HandlePlaybackFinished;
            _playbackTimer.Start();
            _playStopwatch.Start();

            return Task.CompletedTask;
        }

        public Task Pause()
        {
            if (Playing && !Paused)
            {
                waveOut.Pause();
                Paused = true;
                _playbackTimer.Stop();
                _playStopwatch.Stop();
                _playbackTimer.Interval -= _playStopwatch.ElapsedMilliseconds;
            }

            return Task.CompletedTask;
        }

        public Task Resume()
        {
            if (Playing && Paused)
            {
                waveOut.Play();
                Paused = false;
                _playbackTimer.Start();
                _playStopwatch.Reset();
                _playStopwatch.Start();
            }
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (Playing)
            {
                waveOut.Stop();
                Playing = false;
                Paused = false;
                _playbackTimer.Stop();
                _playStopwatch.Stop();
                FileUtil.ClearTempFiles();
            }
            return Task.CompletedTask;
        }

        private void HandlePlaybackFinished(object sender, ElapsedEventArgs e)
        {
            Playing = false;
            PlaybackFinished?.Invoke(this, e);
            _playbackTimer.Dispose();
            _playbackTimer = null;
        }

        private void HandlePlaybackStopped(object sender, StoppedEventArgs e)
        {
            waveOut.Dispose();
        }

        public Task SetVolume(int percent)
        {
            if (percent > 100) percent = 100;
            CurrentVolume = percent;
            // Set the volume
            waveOut.Volume = (float)percent / 100;

            return Task.CompletedTask;
        }

        public WindowsPlayerNAudio()
        {
            waveOut = new WaveOutEvent();
            waveOut.PlaybackStopped += HandlePlaybackStopped;
        }
    }
}