/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        public Gdk.Key Key { get; private set; }
        /// <summary>
        /// The integer key code that represents the key used to stop the sound.
        /// </summary>
        /// <value></value>
        public Gdk.Key? StopKey { get; private set; }

        /// <summary>
        /// The integer key code that represents the key used to instantly stop the sound.
        /// Not generally used. If set, does not prevent the global kill binding from killing this sound.
        /// </summary>
        /// <value></value>
        public Gdk.Key KillKey { get; private set; }

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
            output.AddValue("keyCode", (int)Key);
            output.AddValue("stopKeyCode", (int)StopKey);
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
        public async Task PlaySound(object sender, KeyTriggerEventArgs e)
        {
            Player player = new();
            int initialVolume = FadeInTime >= 100 ? 0 : 100;
            await player.SetVolume(initialVolume);
            SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.Add(player);
            await player.Play(AbsoluteFilePath);
            if (FadeInTime >= 100)
            {
                while (player.CurrentVolume < 100)
                {
                    await player.SetVolume(player.CurrentVolume + 1);
                    await Task.Delay(FadeInTime / 100);
                }
            }
            SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.Remove(player);
        }

        /// <summary>
        /// When attached to a key trigger event, stops or fades out the sound when the key is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public void StopSound(object sender, KeyTriggerEventArgs e)
        {
            if (FadeOutTime >= 100)
            {
                while (player.CurrentVolume > 0)
                {
                    Task.Run(async () => await player.SetVolume(player.CurrentVolume - 1));
                    Thread.Sleep(FadeOutTime / 100);
                }
            }
            Task.Run(async () => await player.Stop());
            SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.Remove(player);
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
            SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.Remove(player);
        }

        public SoundConfiguration(string filePath, Gdk.Key key, Gdk.Key? stopKey = null, string absoluteFilePath = null, int fadeInTime = 0, int fadeOutTime = 0, float maxVolume = 100, float minVolume = 0)
        {
            FilePath = filePath;
            AbsoluteFilePath = absoluteFilePath;

            Key = key;
            StopKey = stopKey;

            FadeInTime = fadeInTime;
            FadeOutTime = fadeOutTime;

            MaxVolume = maxVolume;
            MinVolume = minVolume;

            player = new();

            if (SoundboardConfiguration.CurrentConfig.Keybindings.ContainsKey(key))
            {
                SoundboardConfiguration.CurrentConfig.Keybindings[key].KeyTriggered += (sender, e) => Task.Run(async() => await this.PlaySound(sender, e));
            }
            else
            {
                SoundboardConfiguration.CurrentConfig.Keybindings.Add(key, new Keybinding(key));
                SoundboardConfiguration.CurrentConfig.Keybindings[key].KeyTriggered += (sender, e) => Task.Run(async() => await this.PlaySound(sender, e));
            }
        }

        public override string ToString()
        {
            return $"{FilePath}\t{Key}\t{FadeInTime}\t{FadeOutTime}";
        }
    }
}