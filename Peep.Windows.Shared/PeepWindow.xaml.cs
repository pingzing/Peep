using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Peep.Shared;
using XamlAnimatedGif;

namespace Peep.Windows.Shared;

public partial class PeepWindow : Window
{
    private readonly List<(string, string, Stretch)> _ventressInfo = new()
    {
        ("sounds/ventress/peep.mp3", "gifs/ventress/peep.gif", Stretch.Uniform),
    };

    private readonly List<(string, string, Stretch)> _kawKawInfo = new()
    {
        ("sounds/kawkaw/kawkaw_nyon_1.wav", "gifs/kawkaw/kawkaw_nyon.gif", Stretch.None),
        ("sounds/kawkaw/kawkaw_nyon_2.wav", "gifs/kawkaw/kawkaw_nyon.gif", Stretch.None),
        ("sounds/kawkaw/kawkaw_lick_1.wav", "gifs/kawkaw/kawkaw_lick.gif", Stretch.None),
        ("sounds/kawkaw/kawkaw_lick_2.wav", "gifs/kawkaw/kawkaw_lick.gif", Stretch.None),
        ("sounds/kawkaw/kawkaw_lick_3.wav", "gifs/kawkaw/kawkaw_lick.gif", Stretch.None),
    };

    private MediaPlayer _soundPlayer = new MediaPlayer();
    private Animator _gifController;
    private Random _random = new Random();
    private TaskCompletionSource<bool> _animatorReady = new();

    public PeepWindow()
    {
        InitializeComponent();
    }

    // Fires every time we (re)load the image's UriSource
    private void ImageElement_Loaded(object sender, RoutedEventArgs e)
    {
        _gifController = AnimationBehavior.GetAnimator(ImageElement);
        _animatorReady.SetResult(true);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        WindowExtensions.SetWindowExTransparent(hwnd);
    }

    private (string AudioPath, string VisualPath, Stretch VisualStretch) GetPeepInfo(ChosenCharacter character)
    {
        var targetList = character switch
        {
            ChosenCharacter.Ventress => _ventressInfo,
            ChosenCharacter.KawKaw => _kawKawInfo,
            _ => throw new Exception($"Invalid character: {character}"),
        };

        int randomIndex = _random.Next(0, targetList.Count);
        return targetList[randomIndex];
    }

    public async void Peep(ChosenCharacter chosenCharacter)
    {
        if (Visibility == Visibility.Visible)
        {
            // Don't do anything if the window is already in the middle of peeping.
            return;
        }

        _gifController = null;
        _animatorReady = new TaskCompletionSource<bool>();

        (string audioPath, string visualPath, Stretch visualStretch) = GetPeepInfo(chosenCharacter);
        ImageElement.Stretch = visualStretch;
        _soundPlayer.Open(new Uri(audioPath, UriKind.RelativeOrAbsolute));
        AnimationBehavior.SetSourceUri(ImageElement, new Uri(visualPath, UriKind.Relative));

        // Get DPI scaling
        //find out if our app is being scaled by the monitor
        PresentationSource source = PresentationSource.FromVisual(this);
        double dpiScaling = (
            source != null && source.CompositionTarget != null ? source.CompositionTarget.TransformFromDevice.M11 : 1
        );

        // Get monitor mouse is on.
        var mousePos = Control.MousePosition;
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

        // Wait for the image to finish loading and the GifController to be available.
        await _animatorReady.Task;
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
        await Fade(ImageElement, OpacityProperty, FadeDirection.Out);
        ImageElement.Opacity = 0;

        _gifController.Pause();
        _gifController.Rewind();
        _soundPlayer.Stop();
        _soundPlayer.Position = TimeSpan.Zero;
        _soundPlayer.Close();

        Visibility = Visibility.Collapsed;
    }

    private enum FadeDirection
    {
        In,
        Out,
    }

    private Task Fade(UIElement element, DependencyProperty propertyToFade, FadeDirection direction)
    {
        DoubleAnimation fade = new DoubleAnimation
        {
            From = direction == FadeDirection.In ? 0.0 : 1.0,
            To = direction == FadeDirection.In ? 1.0 : 0.0,
            Duration = TimeSpan.FromMilliseconds(150),
            AutoReverse = false,
            FillBehavior = FillBehavior.Stop,
        };
        var tcs = new TaskCompletionSource<bool>();
        fade.Completed += (s, e) => tcs.SetResult(true);
        element.BeginAnimation(propertyToFade, fade);
        return tcs.Task;
    }
}
