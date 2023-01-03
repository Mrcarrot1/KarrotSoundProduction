/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public string FilePath = null;
        public string Name = "New Soundboard";
        public bool ChangedSinceLastSave { get; private set; }

        public List<SoundConfiguration> Sounds = new List<SoundConfiguration>();

        public Dictionary<Gdk.Key, Keybinding> Keybindings = new Dictionary<Gdk.Key, Keybinding>();

        public List<Player> CurrentlyPlaying = new List<Player>();

        public void AddSound(SoundConfiguration sound)
        {
            Sounds.Add(sound);
            Keybinding binding = null;
            if (!Keybindings.TryGetValue(sound.Key, out binding))
            {
                Keybindings.Add(sound.Key, new(sound.Key));
                binding = Keybindings[sound.Key];
            }
            binding.KeyTriggered += sound.PlaySound;
            ChangedSinceLastSave = true;
        }

        public void RemoveSound(int index)
        {
            SoundConfiguration sound = Sounds[index];
            Keybindings[sound.Key].KeyTriggered -= sound.PlaySound;
            Sounds.RemoveAt(index);
            ChangedSinceLastSave = true;
        }

        public void EditSound(int index, SoundConfiguration sound)
        {
            SoundConfiguration soundBefore = Sounds[index];
            Sounds[index] = sound;
            Keybindings[soundBefore.Key].KeyTriggered -= soundBefore.PlaySound;
            Keybinding binding = null;
            if (!Keybindings.TryGetValue(sound.Key, out binding))
            {
                Keybindings.Add(sound.Key, new(sound.Key));
                binding = Keybindings[sound.Key];
            }
            binding.KeyTriggered += sound.PlaySound;
            ChangedSinceLastSave = true;
        }

        public async Task KillAllSounds()
        {
            foreach (var player in CurrentlyPlaying.ToArray())
            {
                await player.Stop();
            }
            CurrentlyPlaying.RemoveAll(x => true);
        }

        public async void KillAllSounds(object sender, KeyTriggerEventArgs e)
        {
            await KillAllSounds();
        }

        public override string ToString()
        {
            StringBuilder output = new(256);
            foreach (SoundConfiguration sound in Sounds)
            {
                output.Append($"{sound}\n");
            }
            string result = output.ToString().SafeSubstring(0, output.Length - 1).Trim();
            return result == "" ? "No Sounds" : result;
        }

        public static async Task<SoundboardConfiguration> Load(string filePath)
        {
            SoundboardConfiguration output = new();
            output.FilePath = filePath;
            output.Keybindings.Add(Gdk.Key.Tab, new(Gdk.Key.Tab));
            output.Keybindings[Gdk.Key.Tab].KeyTriggered += output.KillAllSounds;

            if (!KONParser.Default.TryParse(File.ReadAllText(filePath), out KONNode node))
            {
                ErrorDialog error = new("This file could not be read as a KSP soundboard file.");
                error.Show();
                return null;
            }

            if (node.Values.ContainsKey("name"))
                output.Name = (string)node.Values["name"];
            else
                output.Name = "Untitled Soundboard";

            if (node.Values.ContainsKey("formatVersion"))
            {
                int formatVersion = (int)node.Values["formatVersion"];
                if (formatVersion > Utils.KSPFormatVersion)
                {
                    WarningDialog dialog = new("This file was created using a newer version of Karrot Sound Production than this version and may not load correctly. Would you like to continue?", "Continue Anyway");
                    if (await dialog.GetResponse() == DialogResponse.Cancel)
                    {
                        return null;
                    }
                }
                if (formatVersion == 0 && Utils.KSPFormatVersion != 0)
                {
                    WarningDialog dialog = new("This file was created using a development version of Karrot Sound Production and may not load correctly. Would you like to continue>", "Continue Anyway");
                    if (await dialog.GetResponse() == DialogResponse.Cancel)
                    {
                        return null;
                    }
                }
            }
            else
            {
                WarningDialog dialog = new("This file was created using an unknown version of Karrot Sound Production and may not load correctly. Would you like to continue?", "Continue Anyway");
                if (await dialog.GetResponse() == DialogResponse.Cancel)
                {
                    return null;
                }
            }

            foreach (KONNode childNode in node.Children)
            {
                if (childNode.Name == "SOUND")
                {
                    string soundPath = null;
                    if (childNode.Values.ContainsKey("filePath"))
                        soundPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filePath), (string)childNode.Values["filePath"]));
                    else if (childNode.Values.ContainsKey("originalFilePath"))
                        soundPath = (string)childNode.Values["originalFilePath"];
                    else
                    {
                        ErrorDialog error = new($"Failed to load sound- file path not found.");
                        error.Show();
                        continue;
                    }

                    Gdk.Key key = 0;
                    if (childNode.Values.ContainsKey("keyCode"))
                        key = (Gdk.Key)(int)childNode.Values["keyCode"];
                    else
                    {
                        WarningDialogNoCancel warning = new($"Sound {soundPath} does not have a configured key code.");
                        warning.Show();
                    }

                    Gdk.Key? stopKey = null;
                    if (childNode.Values.ContainsKey("stopKeyCode"))
                        stopKey = (Gdk.Key)(int)childNode.Values["stopKeyCode"];

                    int fadeInTime = 0;
                    if (childNode.Values.ContainsKey("fadeInTime"))
                        fadeInTime = (int)childNode.Values["fadeInTime"];

                    int fadeOutTime = 0;
                    if (childNode.Values.ContainsKey("fadeOutTime"))
                        fadeOutTime = (int)childNode.Values["fadeOutTime"];

                    float maxVolume = 1;
                    float minVolume = 0;

                    if (childNode.Values.ContainsKey("maxVolume"))
                        maxVolume = (float)childNode.Values["maxVolume"];
                    if (childNode.Values.ContainsKey("minVolume"))
                        minVolume = (float)childNode.Values["minVolume"];

                    string wavePath = Utils.GetWavePath(soundPath);

                    SoundConfiguration sound = new(wavePath, key, stopKey, fadeInTime: fadeInTime, fadeOutTime: fadeOutTime, maxVolume: maxVolume, minVolume: minVolume);
                    output.AddSound(sound);
                }
            }

            return output;
        }

        public void Save() => Save(FilePath);

        public void Save(string filePath)
        {
            FilePath = filePath;
            KONNode node = new("SOUNDBOARD_CONFIGURATION");
            node.AddValue("name", Name);
            node.AddValue("formatVersion", Utils.KSPFormatVersion);
            foreach (SoundConfiguration sound in Sounds)
            {
                node.AddChild(sound.GetNode());
            }
            File.WriteAllText(filePath, KONWriter.Default.Write(node));
            ChangedSinceLastSave = false;
        }
    }
}