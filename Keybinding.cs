using System;
using Gtk;

namespace KarrotSoundProduction
{
    public class Keybinding
    {
        public ushort KeyCode { get; private set; }

        public event EventHandler<KeyTriggerEventArgs> KeyTriggered;

        private protected virtual void OnKeyTrigger(KeyTriggerEventArgs e)
        {
            EventHandler<KeyTriggerEventArgs> raiseEvent = KeyTriggered;

            if(raiseEvent != null)
            {
                raiseEvent(this, e);
            }
        }

        public void TriggerKey()
        {
            OnKeyTrigger(new KeyTriggerEventArgs(KeyCode));
        }

        public Keybinding(ushort keyCode)
        {
            KeyCode = keyCode;
        }
    }
    public class KeyTriggerEventArgs : EventArgs
    {
        public ushort KeyCode;
        public KeyTriggerEventArgs(ushort keyCode)
        {
            KeyCode = keyCode;
        }
    }
}