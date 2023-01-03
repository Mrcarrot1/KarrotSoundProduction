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
using NetCoreAudio;

namespace KarrotSoundProduction
{
    public class ExitConfirmationDialog : Window
    {
        [UI] private Button exitCancelButton = null;
        [UI] private Button exitConfirmButton = null;
        [UI] private Button saveAndExitButton = null;

        public ExitConfirmationDialog() : this(new Builder("MainWindow.glade")) 
        { 
            exitCancelButton.Clicked += Cancel;
            exitConfirmButton.Clicked += Confirm;
            saveAndExitButton.Clicked += SaveAndExit;
        }

        private ExitConfirmationDialog(Builder builder) : base(builder.GetRawOwnedObject("ExitConfirmationDialog"))
        {
            builder.Autoconnect(this);
        }

        private void Cancel(object sender, EventArgs e)
        {
            this.Destroy();
        }

        private async void SaveAndExit(object sender, EventArgs e)
        {
            if (await Program.MainWindow.SaveFile())
                Application.Quit();
        }

        private void Confirm(object sender, EventArgs e)
        {
            Application.Quit();
        }
    }
}