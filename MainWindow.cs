using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace KarrotSoundProduction
{
    class MainWindow : Window
    {
        [UI] private Label _label1 = null;
        [UI] private Button _button1 = null;

        private int _counter;

        public MainWindow() : this(new Builder("MainWindow.glade")) 
        { 
            this.KeyReleaseEvent += Key_Released;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            _button1.Clicked += Button1_Clicked;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void Button1_Clicked(object sender, EventArgs a)
        {
            _counter++;
            
        }

        private void Key_Released(object sender, KeyReleaseEventArgs e)
        {
            _label1.Text = $"You have pressed and released {e.Event.Key} (code {e.Event.HardwareKeycode})";
        }
    }
}
