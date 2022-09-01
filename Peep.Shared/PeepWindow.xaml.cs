using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using XamlAnimatedGif;

namespace Peep.Shared
{
    public partial class PeepWindow : Window
    {
        private MediaPlayer _soundPlayer = new MediaPlayer();
        private Animator _gifController;

        public PeepWindow()
        {
            InitializeComponent();
            _soundPlayer.Open(new Uri("peep.mp3", UriKind.RelativeOrAbsolute));
        }

        private void ImageElement_Loaded(object sender, RoutedEventArgs e)
        {
            _gifController = AnimationBehavior.GetAnimator(ImageElement);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            WindowExtensions.SetWindowExTransparent(hwnd);
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

            // Show the window and play the gif.
            Visibility = Visibility.Visible;
            await Fade(ImageElement, OpacityProperty, FadeDirection.In);
            ImageElement.Opacity = 1;

            _gifController.Play();
        }

        private void ImageElement_AnimationStarted(DependencyObject d, AnimationStartedEventArgs e)
        {
            // Slaved to the gif rather than being run simultaneously,
            // to try to reduce desync.
            _soundPlayer.Play();
        }

        private async void ImageElement_AnimationCompleted(DependencyObject d, AnimationCompletedEventArgs e)
        {
            _gifController.Pause();
            _gifController.Rewind();
            _soundPlayer.Stop();
            _soundPlayer.Position = TimeSpan.Zero;

            await Fade(ImageElement, OpacityProperty, FadeDirection.Out);
            ImageElement.Opacity = 0;
            Visibility = Visibility.Collapsed;
        }

        private enum FadeDirection
        {
            In,
            Out
        }

        private Task Fade(UIElement element, DependencyProperty propertyToFade, FadeDirection direction)
        {
            DoubleAnimation fade = new DoubleAnimation
            {
                From = direction == FadeDirection.In ? 0.0 : 1.0,
                To = direction == FadeDirection.In ? 1.0 : 0.0,
                Duration = TimeSpan.FromMilliseconds(150),
                AutoReverse = false,
                FillBehavior = FillBehavior.Stop
            };
            var tcs = new TaskCompletionSource<bool>();
            fade.Completed += (s, e) => tcs.SetResult(true);
            element.BeginAnimation(propertyToFade, fade);
            return tcs.Task;
        }
    }
}
