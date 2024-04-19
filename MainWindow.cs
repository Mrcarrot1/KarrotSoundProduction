/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Gtk;
using GLib;
using UI = Gtk.Builder.ObjectAttribute;
using Task = System.Threading.Tasks.Task;
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
        [UI] private Button editSoundButton = null;
        [UI] private Button removeSoundButton = null;
        [UI] private ImageMenuItem aboutButton = null;
        [UI] private ImageMenuItem openFileButton = null;
        [UI] private ImageMenuItem newFileButton = null;
        [UI] private ImageMenuItem saveFileButton = null;
        [UI] private ImageMenuItem saveFileAsButton = null;
        [UI] private ImageMenuItem quitButton = null;
        [UI] private Label mainViewLabel = null;
        [UI] private CheckButton playbackEnabledCheck = null;

        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
            this.KeyReleaseEvent += Key_Released;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            SoundboardConfiguration.CurrentConfig = new SoundboardConfiguration();
            SoundboardConfiguration.CurrentConfig.Keybindings.Add(Gdk.Key.b, new Keybinding(Gdk.Key.b));
            SoundboardConfiguration.CurrentConfig.Keybindings[Gdk.Key.b].KeyTriggered +=
            async (object sender, KeyTriggerEventArgs e) =>
            {
                foreach (Player player in SoundboardConfiguration.CurrentConfig.CurrentlyPlaying)
                {
                    await player.SetVolume(50);
                }
            };

            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            _button1.Clicked += Button1_Clicked;
            _button2.Clicked += Button2_Clicked;
            aboutButton.Activated += ShowAbout;
            openFileButton.Activated += ShowOpen;
            addSoundButton.Clicked += AddSoundClicked;
            editSoundButton.Clicked += EditSoundClicked;
            removeSoundButton.Clicked += RemoveSoundClicked;
            saveFileAsButton.Activated += ShowSaveFileAs;
            saveFileButton.Activated += SaveFileClicked;
            quitButton.Activated += QuitButtonClicked;
            newFileButton.Activated += NewButtonClicked;
        }

        private async void Window_DeleteEvent(object sender, DeleteEventArgs e)
        {
            e.RetVal = true;
            await QuitApplication();
        }

        private async void QuitButtonClicked(object sender, EventArgs e)
        {
            await QuitApplication();
        }

        private async void NewButtonClicked(object sender, EventArgs e)
        {
            bool ok = true;
            if (SoundboardConfiguration.CurrentConfig.ChangedSinceLastSave)
            {
                ExitConfirmationDialog dialog = 
                    new("You have unsaved changes. Would you like to continue?", "Continue");
                ok = await dialog.GetResponse();
            }
            if (ok)
            {
                using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
                SoundboardConfiguration.CurrentConfig = new();
                UpdateMainText();
            }
        }

        public async Task<bool> QuitApplication()
        {
            using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            bool output;
            if (SoundboardConfiguration.CurrentConfig.ChangedSinceLastSave)
            {
                ExitConfirmationDialog dialog = new();
                if (await dialog.GetResponse())
                {
                    await SoundboardConfiguration.CurrentConfig.KillAllSounds();
                    Gtk.Application.Quit();
                    output = true;
                }
                else
                {
                    output = false;
                }
            }
            else
            {
                await SoundboardConfiguration.CurrentConfig.KillAllSounds();
                Gtk.Application.Quit();
                output = true;
            }
            return output;
        }

        private void Button1_Clicked(object sender, EventArgs a)
        {
            new PlaySoundDialog().Show();
        }

        private async void Button2_Clicked(object sender, EventArgs a)
        {
            using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            await SoundboardConfiguration.CurrentConfig.KillAllSounds();
        }

        private async void AddSoundClicked(object sender, EventArgs e)
        {
            using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            AddSoundDialog dialog = new();
            dialog.Show();
        }

        private async void EditSoundClicked(object sender, EventArgs e)
        {
            using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            EditSoundDialog dialog = new();
            dialog.Show();
        }

        private async void RemoveSoundClicked(object sender, EventArgs e)
        {
            using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            RemoveSoundDialog dialog = new();
            dialog.Show();
        }

        public async void UpdateMainText()
        {
            using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            mainViewLabel.Text = SoundboardConfiguration.CurrentConfig.ToString();
        }

        private async void Key_Released(object sender, KeyReleaseEventArgs e)
        {
            if (!playbackEnabledCheck.Active) return;
            Console.WriteLine($"Received {e.Event.Key} ({e.Event.HardwareKeycode})");

            var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            bool ok = SoundboardConfiguration.CurrentConfig.Keybindings.TryGetValue(e.Event.Key, out Keybinding value);
            l.Dispose();

            if (ok)
            {
                await value.TriggerKey();
            }
        }

        private async void PlayerFinished(object sender, EventArgs e)
        {
            using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.Remove((Player)sender);
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            AboutDialog aboutDialog = new();
            this.Application.AddWindow(aboutDialog);
            aboutDialog.Show();
        }

        private async void ShowOpen(object sender, EventArgs e)
        {
            using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            if (SoundboardConfiguration.CurrentConfig.ChangedSinceLastSave)
            {
                ExitConfirmationDialog dialog = new("Are you sure you wish to continue? You have unsaved changes.", "Continue");
                bool confirm = await dialog.GetResponse();
                if (!confirm) return;
            }
            FileChooserDialog fileChooser = 
                new("Select File", this, FileChooserAction.Open, "_Cancel", ResponseType.Cancel, "_Open", ResponseType.Accept);
            FileFilter filter = new()
            {
                Name = "KON Soundboard Files"
            };
            filter.AddPattern("*.ksp");
            fileChooser.Filter = filter;
            int response = 32768;
            string path = "";
            do
            {
                response = fileChooser.Run();
                if (response == -6 || response == -4) //Cancel or close
                {
                    fileChooser.Destroy();
                    return;
                }
                if (fileChooser.File != null && (response == -3 || response == 0)) //Open or Accept
                {
                    path = fileChooser.File.Path;
                    fileChooser.Destroy();
                }
            } while (response != 0 && response != -3);
            var loadedConfig = await SoundboardConfiguration.Load(path);
            if (loadedConfig != null)
            {
                SoundboardConfiguration.CurrentConfig = loadedConfig;
                UpdateMainText();
            }
        }

        public async Task<bool> SaveFile()
        {
            using var l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            if (SoundboardConfiguration.CurrentConfig.FilePath != null)
            {
                SoundboardConfiguration.CurrentConfig.Save();
                return true;
            }
            else
            {
                return await SaveFileAs(false);
            }
        }

        public async Task<bool> SaveFileAs(bool getLock = true)
        {
            DisposableLock l = null;
            if (getLock) l = await SoundboardConfiguration.CurrentConfigLockProvider.GetLock();
            FileChooserDialog fileChooser = new("Select Save Location", this, FileChooserAction.Save, "_Cancel", ResponseType.Cancel, "_Save", ResponseType.Accept);
            FileFilter filter = new()
            {
                Name = "KON Soundboard Files"
            };
            filter.AddPattern("*.ksp");
            fileChooser.Filter = filter;
            fileChooser.SetFilename("soundboard.ksp");
            fileChooser.SelectFilename("soundboard.ksp");
            int response = 32768;
            string path = "";
            do
            {
                response = fileChooser.Run();
                if (response == -6 || response == -4) //Cancel or close
                {
                    fileChooser.Destroy();
                    return false;
                }
                if (fileChooser.File != null && (response == -3 || response == -1)) //Open or Accept
                {
                    path = fileChooser.File.Path;
                    fileChooser.Destroy();
                }
            } while (response != -1 && response != -3);
            if (!path.EndsWith(".ksp"))
            {
                path += ".ksp";
            }
            Console.WriteLine(path);
            if (File.Exists(path))
            {
                WarningDialog dialog = new("This file already exists. Would you like to continue?", "Overwrite");
                var wResponse = await dialog.GetResponse();
                if (wResponse == DialogResponse.Cancel) return false;
            }
            SoundboardConfiguration.CurrentConfig.Save(path);
            if (getLock) l.Dispose();
            return true;
        }

        private async void SaveFileClicked(object sender, EventArgs e)
        {
            await SaveFile();
        }

        private async void ShowSaveFileAs(object sender, EventArgs e)
        {
            await SaveFileAs();
        }
    }
}
