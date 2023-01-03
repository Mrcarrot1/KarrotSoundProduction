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
    public class WarningDialogNoCancel : Dialog
    {
        [UI] private Label warningLabel1 = null;
        [UI] private Button warningOkButton1 = null;
        public WarningDialogNoCancel(string message) : this(new Builder("MainWindow.glade"))
        {
            warningOkButton1.Clicked += OK;
            warningLabel1.Text = message;
        }

        private WarningDialogNoCancel(Builder builder) : base(builder.GetRawOwnedObject("ErrorDialog"))
        {
            builder.Autoconnect(this);
        }

        private void OK(object sender, EventArgs e)
        {
            this.Destroy();
        }
    }
}