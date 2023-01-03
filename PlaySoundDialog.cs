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
    public class PlaySoundDialog : Window
    {
        [UI] private Button playCancelButton = null;
        [UI] private Button playConfirmButton = null;
        [UI] private ComboBoxText playSoundSelector = null;

        public PlaySoundDialog() : this(new Builder("MainWindow.glade"))
        {
            playCancelButton.Clicked += Cancel;
            playConfirmButton.Clicked += Confirm;
            playConfirmButton.Sensitive = false;
            foreach (SoundConfiguration sound in SoundboardConfiguration.CurrentConfig.Sounds)
            {
                playSoundSelector.Append($"sound {sound.FilePath}", sound.ToString(false));
            }
            playSoundSelector.Changed += SelectionChanged;
        }

        private PlaySoundDialog(Builder builder) : base(builder.GetRawOwnedObject("PlaySoundDialog"))
        {
            builder.Autoconnect(this);
        }

        private void Cancel(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SelectionChanged(object sender, EventArgs e)
        {
            playConfirmButton.Sensitive = true;
        }

        private async void Confirm(object sender, EventArgs e)
        {
            this.Close();
            var sound = SoundboardConfiguration.CurrentConfig.Sounds[playSoundSelector.Active];
            await Task.Run(() => { sound.PlaySound(sender, new(sound.Key)); });
        }
    }
}