using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;
using NetCoreAudio;

namespace KarrotSoundProduction
{
    class MainWindow : Window
    {
        private Player player = new Player();

        [UI] private Label _label1 = null;
        [UI] private Button _button1 = null;
        [UI] private Button _button2 = null;
        [UI] private Entry _entry = null;

        private int _counter;

        public MainWindow() : this(new Builder("MainWindow.glade")) 
        { 
            this.KeyReleaseEvent += Key_Released;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            DeleteEvent += Button2_Clicked; //Temporary fix so sounds should stop when the window closes
            _button1.Clicked += Button1_Clicked;
            _button2.Clicked += Button2_Clicked;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void Button1_Clicked(object sender, EventArgs a)
        {
            player.Play(_entry.Text);
        }
        private void Button2_Clicked(object sender, EventArgs a)
        {
            player.Stop();
        }

        private void Key_Released(object sender, KeyReleaseEventArgs e)
        {
            _label1.Text = $"You have pressed and released {e.Event.Key} (code {e.Event.HardwareKeycode})";
        }
    }
}
