/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;
using NetCoreAudio;

namespace KarrotSoundProduction
{
    class MainWindow : Window
    {
        [UI] private Label _label1 = null;
        [UI] private Button _button1 = null;
        [UI] private Button _button2 = null;
        [UI] private Entry _entry = null;
        [UI] private Button addSoundButton = null;
        [UI] private Grid mainGrid = null;
        [UI] private ImageMenuItem _aboutButton = null;
        [UI] private ImageMenuItem _openButton = null;

        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
            this.KeyReleaseEvent += Key_Released;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            SoundboardConfiguration.CurrentConfig = new SoundboardConfiguration();
            SoundboardConfiguration.CurrentConfig.Keybindings.Add(Gdk.Key.Tab, new Keybinding(Gdk.Key.Tab));
            SoundboardConfiguration.CurrentConfig.Keybindings[Gdk.Key.Tab].KeyTriggered += KillSoundsKey;

            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            _button1.Clicked += Button1_Clicked;
            _button2.Clicked += Button2_Clicked;
            _aboutButton.Activated += ShowAbout;
            _openButton.Activated += ShowOpen;
            addSoundButton.Clicked += AddSoundClicked;
        }

        private async void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            await KillAllSounds();
            Application.Quit();
        }

        private async void Button1_Clicked(object sender, EventArgs a)
        {
            Player player = new Player();
            var task = player.Play("/home/mrcarrot/Music/Kaguya/S3 Insert- My Nonfiction/01.My Nonfiction.wav");
            player.PlaybackFinished += PlayerFinished;
            SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.Add(player);
            await task;
        }
        private async void Button2_Clicked(object sender, EventArgs a)
        {
            await KillAllSounds();
        }

        private void AddSoundClicked(object sender, EventArgs e)
        {
            Label hotkeyLabel = new("F");
            hotkeyLabel.Name = "hotkeylabeltest";
            Label playingLabel = new("No");
            Label fileNameLabel = new("test.wav");
            mainGrid.Add(hotkeyLabel);
            mainGrid.Add(playingLabel);
            mainGrid.Add(fileNameLabel);
            AddSoundDialog dialog = new();
            dialog.Show();
            mainGrid.InsertRow(0);
        }

        private async void Key_Released(object sender, KeyReleaseEventArgs e)
        {
            Console.WriteLine($"Received {e.Event.Key}");
            if (SoundboardConfiguration.CurrentConfig.Keybindings.ContainsKey(e.Event.Key))
            {
                await SoundboardConfiguration.CurrentConfig.Keybindings[e.Event.Key].TriggerKey();
            }
            //_label1.Text = $"You have pressed and released {e.Event.Key} (code {e.Event.HardwareKeycode})";
        }

        private void PlayerFinished(object sender, EventArgs e)
        {
            SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.Remove((Player)sender);
        }

        private async void KillSoundsKey(object sender, KeyTriggerEventArgs e)
        {
            await KillAllSounds();
        }

        private async Task KillAllSounds()
        {
            foreach (Player player in SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.ToArray())
            {
                await player.Stop();
            }
            SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.RemoveAll(x => true);
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            AboutDialog aboutDialog = new();
            this.Application.AddWindow(aboutDialog);
            aboutDialog.Show();
        }

        private void ShowOpen(object sender, EventArgs e)
        {
            FileChooserDialog fileChooser = new("Select File", this, FileChooserAction.Open, "_Cancel", ResponseType.Cancel, "_Open", ResponseType.Accept);
            int response = fileChooser.Run();
            Console.WriteLine(response);
        }
    }
}
