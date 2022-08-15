using System;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Peep
{
    public partial class PeepWindow : Window
    {
        public PeepWindow()
        {
            InitializeComponent();
            MediaPlayerElement.MediaEnded += MediaPlayerElement_MediaEnded;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            var wih = new WindowInteropHelper(this);
            var windowHwnd = new HWND(wih.Handle);
            var currentMonitor = PInvoke.MonitorFromWindow(windowHwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            var primaryMonitor = PInvoke.MonitorFromWindow(
                new HWND(IntPtr.Zero),
                MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY
            );
            bool isOnPrimary = currentMonitor == primaryMonitor;

            // Force software rendering for this Window if it's not on the primary monitor.
            // Why? There's a bug in MediaElement! It doesn't play MP4 video if it isn't
            // on the primary.
            if (!isOnPrimary)
            {
                var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                if (hwndSource != null)
                {
                    var hwndTarget = hwndSource.CompositionTarget;
                    if (hwndTarget != null)
                    {
                        hwndTarget.RenderMode = RenderMode.SoftwareOnly;
                    }
                }
            }
        }

        private void PeepWindow_Rendered(object sender, EventArgs e) { }

        private void MediaPlayerElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
