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
            confirmButton.Sensitive = false;
            hotkeyRecordButton.Clicked += RecordHotkey;
            this.KeyReleaseEvent += KeyReleased;
            soundFileChooser.SelectionChanged += SelectionChanged;
        }

        private AddSoundDialog(Builder builder) : base(builder.GetRawOwnedObject("AddSoundDialog"))
        {
            builder.Autoconnect(this);
        }

        private void Cancel(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SelectionChanged(object sender, EventArgs e)
        {
            confirmButton.Sensitive = soundFileChooser.File != null;
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
                new ErrorDialog("Please choose a hotkey.").Show();
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
            string originalFileName = System.IO.Path.GetFullPath(soundFileChooser.File.Path);
            string fileName = Utils.GetWavePath(originalFileName);
            Console.WriteLine(Utils.GetFileFormat(fileName));
            
            Console.WriteLine(fileName);
            SoundConfiguration sound = new(fileName, key.Value, null, originalFileName, (int)(fadeInTime * 1000), (int)(fadeOutTime * 1000), 100, 0);
            SoundboardConfiguration.CurrentConfig.AddSound(sound);
            Console.WriteLine(SoundboardConfiguration.CurrentConfig);
            this.Close();
            Program.MainWindow.UpdateMainText();
        }
    }
}