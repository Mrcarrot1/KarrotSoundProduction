/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.Threading.Tasks;
using Gtk;

namespace KarrotSoundProduction
{
    public class Keybinding
    {
        public Gdk.Key Key { get; private set; }

        public event EventHandler<KeyTriggerEventArgs> KeyTriggered;

        private protected virtual async Task OnKeyTrigger(KeyTriggerEventArgs e)
        {
            EventHandler<KeyTriggerEventArgs> raiseEvent = KeyTriggered;

            if (raiseEvent != null)
            {
                await Task.Run(() => { raiseEvent(this, e); });
            }
        }

        public async Task TriggerKey()
        {
            await OnKeyTrigger(new KeyTriggerEventArgs(Key));
        }

        public Keybinding(Gdk.Key key)
        {
            Key = key;
        }
    }

    /// <summary>
    /// Represents a key release event.
    /// </summary>
    public class KeyTriggerEventArgs : EventArgs
    {
        public Gdk.Key Key;
        public KeyTriggerEventArgs(Gdk.Key key)
        {
            Key = key;
        }
    }
}