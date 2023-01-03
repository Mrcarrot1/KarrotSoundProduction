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
    public class EditSoundDialog : Window
    {
        [UI] private Button editCancelButton = null;
        [UI] private Button editConfirmButton = null;
        [UI] private Entry editFadeInTimeEntry = null;
        [UI] private Entry editFadeOutTimeEntry = null;
        [UI] private ComboBoxText editSoundSelector = null;
        [UI] private Label editHotkeyLabel = null;
        [UI] private Button editHotkeyRecordButton = null;

        private Gdk.Key? key = null;

        private SoundConfiguration currentSound = null;

        private bool recordingKey = false;

        public EditSoundDialog() : this(new Builder("MainWindow.glade"))
        {
            editCancelButton.Clicked += Cancel;
            editConfirmButton.Clicked += Confirm;
            editConfirmButton.Sensitive = false;
            editHotkeyRecordButton.Clicked += RecordHotkey;
            editHotkeyRecordButton.Sensitive = false;
            this.KeyReleaseEvent += KeyReleased;
            foreach (SoundConfiguration sound in SoundboardConfiguration.CurrentConfig.Sounds)
            {
                editSoundSelector.Append($"sound {sound.FilePath}", sound.ToString(false));
            }
            editSoundSelector.Changed += SelectionChanged;
        }

        private EditSoundDialog(Builder builder) : base(builder.GetRawOwnedObject("EditSoundDialog"))
        {
            builder.Autoconnect(this);
        }

        private void Cancel(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SelectionChanged(object sender, EventArgs e)
        {
            currentSound = SoundboardConfiguration.CurrentConfig.Sounds[editSoundSelector.Active];
            editHotkeyRecordButton.Sensitive = true;
            editConfirmButton.Sensitive = true;
            key = currentSound.Key;
            editHotkeyLabel.Text = key.ToString();
            editFadeInTimeEntry.Text = currentSound.FadeInTime.ToString();
            editFadeOutTimeEntry.Text = currentSound.FadeOutTime.ToString();
        }

        private void KeyReleased(object sender, KeyReleaseEventArgs e)
        {
            if (recordingKey)
            {
                key = e.Event.Key;
                editHotkeyLabel.Text = key.ToString();
                recordingKey = false;
            }
        }

        private void RecordHotkey(object sender, EventArgs e)
        {
            editHotkeyLabel.Text = "Press a Key";
            recordingKey = true;
        }

        private void Confirm(object sender, EventArgs e)
        {
            float fadeInTime = 0;
            float fadeOutTime = 0;
            if (!float.TryParse(editFadeInTimeEntry.Text, out fadeInTime))
            {
                new ErrorDialog("Invalid fade in time.").Show();
                return;
            }
            if (!float.TryParse(editFadeOutTimeEntry.Text, out fadeOutTime))
            {
                new ErrorDialog("Invalid fade out time.").Show();
                return;
            }
            
            SoundConfiguration sound = new(currentSound.FilePath, key.Value, null, currentSound.OriginalFilePath, (int)(fadeInTime * 1000), (int)(fadeOutTime * 1000), 100, 0);
            SoundboardConfiguration.CurrentConfig.EditSound(editSoundSelector.Active, sound);
            Console.WriteLine(SoundboardConfiguration.CurrentConfig);
            this.Close();
            Program.MainWindow.UpdateMainText();
        }
    }
}