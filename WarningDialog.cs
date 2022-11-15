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
    public class WarningDialog : Dialog
    {
        [UI] private Label warningLabel = null;
        [UI] private Button warningOkButton = null;
        [UI] private Button warningCancelButton = null;

        public EventHandler Ignored;

        public WarningDialog(string message, string okButtonText = "OK") : this(new Builder("MainWindow.glade"))
        {
            warningOkButton.Clicked += OK;
            warningOkButton.Label = okButtonText;
            warningLabel.Text = message;
            warningCancelButton.Clicked += Cancel;
        }

        private WarningDialog(Builder builder) : base(builder.GetRawOwnedObject("WarningDialog"))
        {
            builder.Autoconnect(this);
        }

        private void OK(object sender, EventArgs e)
        {
            Ignored.Invoke(sender, e);
            this.Destroy();
        }

        private void Cancel(object sender, EventArgs e)
        {
            this.Destroy();
        }
    }
}