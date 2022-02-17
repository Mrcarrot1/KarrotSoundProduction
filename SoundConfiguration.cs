using System;
using System.Collections.Generic;
using System.IO;
using Gtk;
using KarrotObjectNotation;
using System.Threading.Tasks;
using System.Threading;
using NetCoreAudio;

namespace KarrotSoundProduction
{
    /// <summary>
    /// Represents the configuration for a sound, including the file path, keybinding, and other properties.
    /// </summary>
    public class SoundConfiguration
    {
        private Player player;

        /// <summary>
        /// The file path to the sound in question, relative to the location of the configuration file.
        /// </summary>
        /// <value></value>
        public string FilePath { get; private set; }
        /// <summary>
        /// The absolute file path to the sound in question, used only as a fallback if the relative file path is not found.
        /// </summary>
        /// <value></value>
        public string AbsoluteFilePath { get; private set; }
        /// <summary>
        /// The integer key code that represents the key used to play the sound.
        /// </summary>
        /// <value></value>
        public ushort KeyCode { get; private set; }
        /// <summary>
        /// The integer key code that represents the key used to stop the sound.
        /// </summary>
        /// <value></value>
        public ushort StopKeyCode { get; private set; }

        /// <summary>
        /// The integer key code that represents the key used to instantly stop the sound.
        /// Not generally used. If set, does not prevent the global kill binding from killing this sound.
        /// </summary>
        /// <value></value>
        public ushort KillKeyCode { get; private set; }

        /// <summary>
        /// The time, in milliseconds, that it takes to fade the sound in.
        /// </summary>
        /// <value></value>
        public int FadeInTime { get; private set; }
        /// <summary>
        /// The time, in milliseconds, that the sound will fade out either at the end or if the stop key is pressed.
        /// </summary>
        /// <value></value>
        public int FadeOutTime { get; private set; }

        /// <summary>
        /// The maximum volume, in percent, that the sound will reach when fading in, or the volume the sound will be played at.
        /// </summary>
        /// <value></value>
        public float MaxVolume { get; private set; }
        /// <summary>
        /// The minimum volume, in percent, that the sound will start at when fading in.
        /// </summary>
        /// <value></value>
        public float MinVolume { get; private set; }

        /// <summary>
        /// Gets the KONNode object that represents this sound configuration.
        /// </summary>
        /// <returns></returns>
        public KONNode GetNode()
        {
            KONNode output = new KONNode("SOUND");
            output.AddValue("filePath", FilePath);
            output.AddValue("absoluteFilePath", AbsoluteFilePath);
            output.AddValue("keyCode", KeyCode);
            output.AddValue("stopKeyCode", StopKeyCode);
            output.AddValue("fadeInTime", FadeInTime);
            output.AddValue("fadeOutTime", FadeOutTime);
            output.AddValue("maxVolume", MaxVolume);
            output.AddValue("minVolume", MinVolume);

            return output;
        }

        /// <summary>
        /// When attached to a key trigger event, starts the sound when the key is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public void PlaySound(object sender, KeyTriggerEventArgs e)
        {
            Task.Run(async () => await player.SetVolume(0));
            Task.Run(async () => await player.Play(AbsoluteFilePath));
            while(player.CurrentVolume < 100)
            {
                Task.Run(async () => await player.SetVolume(player.CurrentVolume + 1));
                Thread.Sleep(FadeInTime / 100);
            }
        }

        /// <summary>
        /// When attached to a key trigger event, stops or fades out the sound when the key is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public void StopSound(object sender, KeyTriggerEventArgs e)
        {
            
        }

        /// <summary>
        /// When attached to a key trigger event, instantly stops the sound when the key is pressed, regardless of the configured fade out time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public void KillSound(object sender, KeyTriggerEventArgs e)
        {
            Task.Run(async () => await player.Stop());
        }

        public SoundConfiguration(string filePath, ushort keyCode, ushort stopKeyCode = 0, string absoluteFilePath = null, int fadeInTime = 0, int fadeOutTime = 0, float maxVolume = 100, float minVolume = 0)
        {
            FilePath = filePath;
            AbsoluteFilePath = absoluteFilePath;

            KeyCode = keyCode;
            StopKeyCode = stopKeyCode;

            FadeInTime = fadeInTime;
            FadeOutTime = fadeOutTime;

            MaxVolume = maxVolume;
            MinVolume = minVolume;

            if(SoundboardConfiguration.CurrentConfig.Keybindings.ContainsKey(keyCode))
            {
                SoundboardConfiguration.CurrentConfig.Keybindings[keyCode].KeyTriggered += this.PlaySound;
            }
            else
            {
                SoundboardConfiguration.CurrentConfig.Keybindings.Add(keyCode, new Keybinding(keyCode));
                SoundboardConfiguration.CurrentConfig.Keybindings[keyCode].KeyTriggered += PlaySound;
            }
        }
    }
}