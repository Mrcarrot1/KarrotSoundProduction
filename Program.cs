/*  
*  This Source Code Form is subject to the terms of the Mozilla Public
*  License, v. 2.0. If a copy of the MPL was not distributed with this
*  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Gtk;
using Gdk;

namespace KarrotSoundProduction
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine();
            Application.Init();

            var app = new Application("com.calebmharper.ksp", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            var win = new MainWindow();
            win.Title = "KarrotSoundProduction";
            app.AddWindow(win);

            SoundboardConfiguration.CurrentConfig.Keybindings.Add(Gdk.Key.a, new Keybinding(Gdk.Key.A));
            SoundboardConfiguration.CurrentConfig.Keybindings[Gdk.Key.a].KeyTriggered += DoThing;

            Console.WriteLine($"Player backend: {new NetCoreAudio.Player().GetPlayerBackend()}");

            win.Show();
            Application.Run();
            NetCoreAudio.Utils.FileUtil.ClearTempFiles();
            SoundboardConfiguration.CurrentConfig.CurrentlyPlaying.ForEach(async x => await x.Stop());
        }

        static void DoThing(object sender, KeyTriggerEventArgs e)
        {
            Console.WriteLine("A has been pressed");
        }
    }
}