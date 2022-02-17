using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;
using NetCoreAudio;

namespace KarrotSoundProduction
{
    class MainWindow : Window
    {
        private List<Player> currentlyPlaying = new List<Player>();

        [UI] private Label _label1 = null;
        [UI] private Button _button1 = null;
        [UI] private Button _button2 = null;
        [UI] private Entry _entry = null;

        public MainWindow() : this(new Builder("MainWindow.glade")) 
        { 
            this.KeyReleaseEvent += Key_Released;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            SoundboardConfiguration.CurrentConfig = new SoundboardConfiguration();
            SoundboardConfiguration.CurrentConfig.Keybindings.Add(23, new Keybinding(23));
            SoundboardConfiguration.CurrentConfig.Keybindings[23].KeyTriggered += KillSoundsKey;

            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            _button1.Clicked += Button1_Clicked;
            _button2.Clicked += Button2_Clicked;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            KillAllSounds();
            Application.Quit();
        }

        private void Button1_Clicked(object sender, EventArgs a)
        {
            Player player = new Player();
            Task.Run(async () =>
            {
                await player.Play(_entry.Text);
            });
            player.PlaybackFinished += PlayerFinished;
            currentlyPlaying.Add(player);
        }
        private void Button2_Clicked(object sender, EventArgs a)
        {
            KillAllSounds();
        }

        private void Key_Released(object sender, KeyReleaseEventArgs e)
        {
            if(SoundboardConfiguration.CurrentConfig.Keybindings.ContainsKey(e.Event.HardwareKeycode))
            {
                SoundboardConfiguration.CurrentConfig.Keybindings[e.Event.HardwareKeycode].TriggerKey();
            }
            _label1.Text = $"You have pressed and released {e.Event.Key} (code {e.Event.HardwareKeycode})";
        }

        private void PlayerFinished(object sender, EventArgs e)
        {
            currentlyPlaying.Remove((Player)sender);
        }

        private void KillSoundsKey(object sender, KeyTriggerEventArgs e)
        {
            KillAllSounds();
        }

        private void KillAllSounds()
        {
            foreach(Player player in currentlyPlaying.ToArray())
            {
                Task.Run(async () =>
                {
                    await player.Stop();
                });
            }
        }
    }
}
