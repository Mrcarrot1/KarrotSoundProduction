using System;
using System.Collections.Generic;
using KarrotObjectNotation;

namespace KarrotSoundProduction
{
    /// <summary>
    /// Represents the configuration of a soundboard, including sounds and keybindings.
    /// </summary>
    public class SoundboardConfiguration
    {
        public static SoundboardConfiguration CurrentConfig { get; internal set; }

        public List<SoundConfiguration> Sounds = new List<SoundConfiguration>();
        
        public Dictionary<ushort, Keybinding> Keybindings = new Dictionary<ushort, Keybinding>();
    }
}