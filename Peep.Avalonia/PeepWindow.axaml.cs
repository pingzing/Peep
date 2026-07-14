using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using NAudio.Wave;
using Peep.Avalonia.Platform;
using Peep.Shared;

namespace Peep.Avalonia;

public partial class PeepWindow : Window
{
    private record struct PeepInfo(string AudioPath, string VisualPath, Stretch VisualStretch);

    private readonly List<PeepInfo> _ventressInfo =
    [
        new("sounds/ventress/peep.mp3", "gifs/ventress/peep.gif", Stretch.Uniform),
    ];

    private readonly List<PeepInfo> _kawKawInfo =
    [
        new("sounds/kawkaw/kawkaw_nyon_1.wav", "gifs/kawkaw/kawkaw_nyon.gif", Stretch.None),
        new("sounds/kawkaw/kawkaw_nyon_2.wav", "gifs/kawkaw/kawkaw_nyon.gif", Stretch.None),
        new("sounds/kawkaw/kawkaw_lick_1.wav", "gifs/kawkaw/kawkaw_lick.gif", Stretch.None),
        new("sounds/kawkaw/kawkaw_lick_2.wav", "gifs/kawkaw/kawkaw_lick.gif", Stretch.None),
        new("sounds/kawkaw/kawkaw_lick_3.wav", "gifs/kawkaw/kawkaw_lick.gif", Stretch.None),
    ];

    private IWindowExtensions? _windowExtensions;
    private IWavePlayer? _waveOut;
    private WaveStream? _audioReader;
    private readonly Dictionary<string, LoadedGif> _loadedGifs = new();

    public PeepWindow()
    {
        InitializeComponent();
#if WINDOWS
        _windowExtensions = new WindowsWindowExtensions();
#endif
        if (OperatingSystem.IsMacOS())
        {
            _windowExtensions = new MacOSWindowExtensions();
        }
    }

    private PeepInfo GetPeepInfo(ChosenCharacter character)
    {
        var targetList = character switch
        {
            ChosenCharacter.Ventress => _ventressInfo,
            ChosenCharacter.KawKaw => _kawKawInfo,
            _ => throw new Exception($"Invalid character: {character}"),
        };

        int randomIndex = Random.Shared.Next(0, targetList.Count);
        return targetList[randomIndex];
    }

    public async Task Peep(ChosenCharacter chosenCharacter, PixelPoint mousePosition)
    {
        if (IsVisible)
        {
            return;
        }

        ImageElement.Opacity = 0;

        PeepInfo peepInfo = GetPeepInfo(chosenCharacter);

        // Lazy-load the gif if we haven't used it before.
        LoadedGif loadedGif = _loadedGifs.GetOrCreate(
            peepInfo.VisualPath,
            () =>
            {
                using var stream = AssetLoader.Open(new Uri($"avares://Peep.Avalonia/{peepInfo.VisualPath}"));
                return LoadedGif.Decode(stream);
            }
        );

        // Center on the monitor the mouse is currently on
        var screen = Screens.ScreenFromPoint(mousePosition) ?? Screens.Primary;
        if (screen != null)
        {
            var workArea = screen.WorkingArea;
            int windowPixelWidth = (int)Math.Round(Width * screen.Scaling);
            int windowPixelHeight = (int)Math.Round(Height * screen.Scaling);
            int x = workArea.X + (workArea.Width - windowPixelWidth) / 2;
            int y = workArea.Y + (workArea.Height - windowPixelHeight) / 2;
            Position = new PixelPoint(x, y);
        }

        ImageElement.Stretch = peepInfo.VisualStretch;
        ImageElement.RepeatBehavior = GifRepeatBehavior.None;
        ImageElement.SetGif(loadedGif);

        IsVisible = true;
        _windowExtensions?.SetHitTransparent(this);

        ImageElement.Opacity = 1;
        await Task.Delay(150); // <-- Opacity transition duration

        ImageElement.Start();

        // Play sound, slaved to animation start to reduce desync
        using var audioStream = AssetLoader.Open(new Uri($"avares://Peep.Avalonia/{peepInfo.AudioPath}"));
        var audioMs = new MemoryStream();
        await audioStream.CopyToAsync(audioMs);
        audioMs.Position = 0;

        _audioReader = Path.GetExtension(peepInfo.AudioPath).Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            ? new Mp3FileReader(audioMs)
            : new WaveFileReader(audioMs);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_audioReader);
        _waveOut.Play();

        // Wait for one GIF loop to finish
        await Task.Delay(ImageElement.TotalDuration);

        ImageElement.Stop();
        ImageElement.Opacity = 0;
        await Task.Delay(150); // <-- Opacity transition duration

        _waveOut.Stop();
        _waveOut.Dispose();
        _waveOut = null;
        _audioReader.Dispose();
        _audioReader = null;

        IsVisible = false;
    }
}
