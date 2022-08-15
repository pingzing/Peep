using System;
using System.Windows;

namespace Peep
{
    public partial class PeepWindow : Window
    {
        public PeepWindow()
        {
            InitializeComponent();
            MediaPlayerElement.MediaEnded += MediaPlayerElement_MediaEnded;
        }

        private void PeepWindow_Rendered(object sender, EventArgs e) { }

        private void MediaPlayerElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
