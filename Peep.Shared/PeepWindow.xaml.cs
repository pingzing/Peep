using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Brush = System.Windows.Media.Brush;

namespace Peep.Shared
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
            // TODO: Might not need this.
        }

        public async void Peep()
        {
            if (Visibility == Visibility.Visible)
            {
                // Don't do anything if the window is already in the middle of peeping.
                return;
            }

            // Get DPI scaling
            //find out if our app is being scaled by the monitor
            PresentationSource source = PresentationSource.FromVisual(this);
            double dpiScaling = (
                source != null && source.CompositionTarget != null
                    ? source.CompositionTarget.TransformFromDevice.M11
                    : 1
            );

            // Get monitor mouse is on.
            var mousePos = System.Windows.Forms.Control.MousePosition;
            var mouseScreen = Screen.FromPoint(mousePos);

            // Get DPI-scaled width and height of the current monitor's Work Area (i.e. bounds minus taskbars etc)
            Rectangle workArea = mouseScreen.WorkingArea;
            var workAreaWidth = (int)Math.Floor(workArea.Width * dpiScaling);
            var workAreaHeight = (int)Math.Floor(workArea.Height * dpiScaling);

            // Move window to the center of the mouse's screen
            Left = ((workAreaWidth - (Width * dpiScaling)) / 2) + (workArea.Left * dpiScaling);
            Top = ((workAreaHeight - (Height * dpiScaling)) / 2) + (workArea.Top * dpiScaling);

            // Force software rendering for this Window if it's not on the primary monitor.
            // Why? There's a bug in MediaElement! It doesn't play MP4 video if it isn't
            // on the primary.
            SetDesiredRenderMode(mouseScreen);

            // Show the window and play the video.
            Visibility = Visibility.Visible;
            await Task.WhenAll(
                Fade(WindowBrush, Brush.OpacityProperty, FadeDirection.In),
                Fade(MediaPlayerElement, OpacityProperty, FadeDirection.In)
            );

            MediaPlayerElement.Visibility = Visibility.Visible;
            MediaPlayerElement.Play();
        }

        private enum FadeDirection
        {
            In,
            Out
        }

        private Task Fade(Animatable element, DependencyProperty propertyToFade, FadeDirection direction)
        {
            DoubleAnimation fade = ConstructFadeAnimation(direction);
            var tcs = new TaskCompletionSource<bool>();
            fade.Completed += (s, e) => tcs.SetResult(true);
            element.BeginAnimation(propertyToFade, fade);
            return tcs.Task;
        }

        private Task Fade(UIElement element, DependencyProperty propertyToFade, FadeDirection direction)
        {
            DoubleAnimation fade = ConstructFadeAnimation(direction);
            var tcs = new TaskCompletionSource<bool>();
            fade.Completed += (s, e) => tcs.SetResult(true);
            element.BeginAnimation(propertyToFade, fade);
            return tcs.Task;
        }

        private DoubleAnimation ConstructFadeAnimation(FadeDirection direction)
        {
            DoubleAnimation fadeAnimation = new DoubleAnimation();
            fadeAnimation.From = direction == FadeDirection.In ? 0.0 : 1.0;
            fadeAnimation.To = direction == FadeDirection.In ? 1.0 : 0.0;
            fadeAnimation.Duration = TimeSpan.FromMilliseconds(150);
            fadeAnimation.AutoReverse = false;
            return fadeAnimation;
        }

        private void SetDesiredRenderMode(Screen screenWithMose)
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null)
            {
                var hwndTarget = hwndSource.CompositionTarget;
                if (hwndTarget != null)
                {
                    bool isOnPrimary = screenWithMose.Primary;

                    if (!isOnPrimary)
                    {
                        hwndTarget.RenderMode = RenderMode.SoftwareOnly;
                    }
                    else
                    {
                        hwndTarget.RenderMode = RenderMode.Default;
                    }
                }
            }
        }

        private async void MediaPlayerElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaPlayerElement.Stop();
            MediaPlayerElement.Position = TimeSpan.FromMilliseconds(1);
            MediaPlayerElement.Visibility = Visibility.Collapsed;

            await Task.WhenAll(
                Fade(WindowBrush, Brush.OpacityProperty, FadeDirection.Out),
                Fade(MediaPlayerElement, OpacityProperty, FadeDirection.Out)
            );

            this.Visibility = Visibility.Collapsed;
        }
    }
}
