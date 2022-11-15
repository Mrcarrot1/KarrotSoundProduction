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
    public class AboutDialog : Window
    {
        public AboutDialog() : this(new Builder("MainWindow.glade")) { }

        private AboutDialog(Builder builder) : base(builder.GetRawOwnedObject("AboutDialog"))
        {
            builder.Autoconnect(this);
        }
    }
}