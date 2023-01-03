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
    public class RemoveSoundDialog : Window
    {
        [UI] private Button removeCancelButton = null;
        [UI] private Button removeConfirmButton = null;
        [UI] private ComboBoxText removeSoundSelector = null;

        public RemoveSoundDialog() : this(new Builder("MainWindow.glade"))
        {
            removeCancelButton.Clicked += Cancel;
            removeConfirmButton.Clicked += Confirm;
            removeConfirmButton.Sensitive = false;
            foreach (SoundConfiguration sound in SoundboardConfiguration.CurrentConfig.Sounds)
            {
                removeSoundSelector.Append($"sound {sound.FilePath}", sound.ToString(false));
            }
            removeSoundSelector.Changed += SelectionChanged;
        }

        private RemoveSoundDialog(Builder builder) : base(builder.GetRawOwnedObject("RemoveSoundDialog"))
        {
            builder.Autoconnect(this);
        }

        private void Cancel(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SelectionChanged(object sender, EventArgs e)
        {
            removeConfirmButton.Sensitive = true;
        }

        private void Confirm(object sender, EventArgs e)
        {
            SoundboardConfiguration.CurrentConfig.RemoveSound(removeSoundSelector.Active);
            Console.WriteLine(SoundboardConfiguration.CurrentConfig);
            this.Close();
            Program.MainWindow.UpdateMainText();
        }
    }
}