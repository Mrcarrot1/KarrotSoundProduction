/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.Text;
using System.Collections.Generic;
using KarrotObjectNotation;
using NetCoreAudio;

namespace KarrotSoundProduction
{
    /// <summary>
    /// Represents the configuration of a soundboard, including sounds and keybindings.
    /// </summary>
    public class SoundboardConfiguration
    {
        public static SoundboardConfiguration CurrentConfig { get; internal set; }

        public List<SoundConfiguration> Sounds = new List<SoundConfiguration>();

        public Dictionary<Gdk.Key, Keybinding> Keybindings = new Dictionary<Gdk.Key, Keybinding>();

        public List<Player> CurrentlyPlaying = new List<Player>();

        public override string ToString()
        {
            StringBuilder output = new(256);
            foreach (SoundConfiguration sound in Sounds)
            {
                output.Append($"{sound}\n");
            }
            return output.ToString().Substring(0, output.Length - 1);
        }
    }
}