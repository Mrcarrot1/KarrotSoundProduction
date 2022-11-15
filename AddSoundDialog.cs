/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;
using System.IO;
using System.Diagnostics;
using NetCoreAudio;

namespace KarrotSoundProduction
{
    public class AddSoundDialog : Window
    {
        [UI] private Button cancelButton = null;
        [UI] private Button confirmButton = null;
        [UI] private Entry fadeInTimeEntry = null;
        [UI] private Entry fadeOutTimeEntry = null;
        [UI] private FileChooserButton soundFileChooser = null;
        [UI] private Label hotkeyLabel = null;
        [UI] private Button hotkeyRecordButton = null;

        private Gdk.Key? key = null;

        private bool recordingKey = false;

        public AddSoundDialog() : this(new Builder("MainWindow.glade"))
        {
            cancelButton.Clicked += Cancel;
            confirmButton.Clicked += Confirm;
            hotkeyRecordButton.Clicked += RecordHotkey;
            this.KeyReleaseEvent += KeyReleased;
        }

        private AddSoundDialog(Builder builder) : base(builder.GetRawOwnedObject("AddSoundDialog"))
        {
            builder.Autoconnect(this);
        }

        private void Cancel(object sender, EventArgs e)
        {
            this.Close();
        }

        private void KeyReleased(object sender, KeyReleaseEventArgs e)
        {
            if (recordingKey)
            {
                key = e.Event.Key;
                hotkeyLabel.Text = key.ToString();
                recordingKey = false;
            }
        }

        private void RecordHotkey(object sender, EventArgs e)
        {
            hotkeyLabel.Text = "Recording";
            recordingKey = true;
        }

        private void Confirm(object sender, EventArgs e)
        {
            float fadeInTime = 0;
            float fadeOutTime = 0;
            if (soundFileChooser.File == null)
            {
                new ErrorDialog("A sound file is required.").Show();
                return;
            }
            if (key == null && !(sender is Button button && button.Name == "warningOkButton"))
            {
                WarningDialog dialog = new WarningDialog("Please choose a hotkey.", "Ignore");
                dialog.Ignored += this.Confirm;
                dialog.Show();
                return;
            }
            if (!float.TryParse(fadeInTimeEntry.Text, out fadeInTime))
            {
                new ErrorDialog("Invalid fade in time.").Show();
                return;
            }
            if (!float.TryParse(fadeInTimeEntry.Text, out fadeOutTime))
            {
                new ErrorDialog("Invalid fade out time.").Show();
                return;
            }
            string fileName = soundFileChooser.File.Path;
            if (System.IO.Path.GetExtension(fileName).ToLower() == ".flac")
            {
                if (!Directory.Exists("temp"))
                {
                    Directory.CreateDirectory("temp");
                }
                Console.WriteLine($"Decoding {fileName}");
                Stopwatch stopwatch = new();
                stopwatch.Start();
                string wavFileName = $"temp/{System.IO.Path.GetFileName(System.IO.Path.ChangeExtension(fileName, ".wav"))}";
                if (!File.Exists(wavFileName))
                    Process.Start($"flac", $"-fd \"{fileName}\" -o \"{wavFileName}\"").WaitForExit();
                stopwatch.Stop();
                Console.WriteLine($"Decode elapsed time: {stopwatch.ElapsedMilliseconds} ms");
                fileName = wavFileName;
            }
            SoundConfiguration sound = new(fileName, key.Value, null, soundFileChooser.File.Path, (int)(fadeInTime * 1000), (int)(fadeOutTime * 1000), 100, 0);
            SoundboardConfiguration.CurrentConfig.Sounds.Add(sound);
            Console.WriteLine(SoundboardConfiguration.CurrentConfig);
            this.Close();
        }
    }
}