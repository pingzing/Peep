using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AnimatedImage.Avalonia;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using NAudio.Wave;
using Peep.Shared;
using SkiaSharp;

namespace Peep.Avalonia;

public partial class PeepWindow : Window
{
    private record struct PeepInfo(string AudioPath, string VisualPath, Stretch VisualStretch, double SpeedRatio);

    private readonly List<PeepInfo> _ventressInfo =
    [
        new("sounds/ventress/peep.mp3", "gifs/ventress/peep.gif", Stretch.Uniform, 5.0),
    ];

    private readonly List<PeepInfo> _kawKawInfo =
    [
        new("sounds/kawkaw/kawkaw_nyon_1.wav", "gifs/kawkaw/kawkaw_nyon.gif", Stretch.None, 1.0),
        new("sounds/kawkaw/kawkaw_nyon_2.wav", "gifs/kawkaw/kawkaw_nyon.gif", Stretch.None, 1.0),
        new("sounds/kawkaw/kawkaw_lick_1.wav", "gifs/kawkaw/kawkaw_lick.gif", Stretch.None, 1.0),
        new("sounds/kawkaw/kawkaw_lick_2.wav", "gifs/kawkaw/kawkaw_lick.gif", Stretch.None, 1.0),
        new("sounds/kawkaw/kawkaw_lick_3.wav", "gifs/kawkaw/kawkaw_lick.gif", Stretch.None, 1.0),
    ];

    private IWavePlayer? _waveOut;
    private WaveStream? _audioReader;
    private readonly Dictionary<string, TimeSpan> _gifDurations = new();

    public PeepWindow()
    {
        InitializeComponent();

        // Pre-compute durations for all distinct GIFs at startup
        var distinctGifPaths = new HashSet<string>();
        foreach (var entry in _ventressInfo)
        {
            distinctGifPaths.Add(entry.VisualPath);
        }
        foreach (var entry in _kawKawInfo)
        {
            distinctGifPaths.Add(entry.VisualPath);
        }

        foreach (var path in distinctGifPaths)
        {
            var uri = new Uri($"avares://Peep.Avalonia/{path}");
            _gifDurations[path] = GetGifDuration(uri);
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

    private static TimeSpan GetGifDuration(Uri gifUri)
    {
        using var stream = AssetLoader.Open(gifUri);

        using var codec = SKCodec.Create(stream);
        if (codec == null)
        {
            return TimeSpan.Zero;
        }

        int totalMs = 0;
        var frameInfos = codec.FrameInfo;
        for (int i = 0; i < frameInfos.Length; i++)
        {
            totalMs += frameInfos[i].Duration;
        }

        return TimeSpan.FromMilliseconds(totalMs);
    }

    public async Task Peep(ChosenCharacter chosenCharacter, PixelPoint mousePosition)
    {
        if (IsVisible)
        {
            return;
        }

        ImageElement.Opacity = 0;

        (string audioPath, string visualPath, Stretch visualStretch, double speedRatio) = GetPeepInfo(chosenCharacter);

        var gifUri = new Uri($"avares://Peep.Avalonia/{visualPath}");

        // Center on the monitor the mouse is currently on
        var screen = Screens.ScreenFromPoint(mousePosition) ?? Screens.Primary;
        if (screen != null)
        {
            var workArea = screen.WorkingArea;
            int windowPixelWidth = (int)Math.Round(ClientSize.Width * screen.Scaling);
            int windowPixelHeight = (int)Math.Round(ClientSize.Height * screen.Scaling);
            int x = workArea.X + (workArea.Width - windowPixelWidth) / 2;
            int y = workArea.Y + (workArea.Height - windowPixelHeight) / 2;
            Position = new PixelPoint(x, y);
        }

        ImageElement.Stretch = visualStretch;
        ImageBehavior.SetSpeedRatio(ImageElement, 0);
        ImageBehavior.SetAnimatedSource(ImageElement, AnimatedImageSourceConverter.Convert(gifUri.ToString()));
        ImageBehavior.SetRepeatBehavior(ImageElement, RepeatBehavior.Forever); // <-- prevent GIF from finishing and freezing on first frame

        TimeSpan visualDuration = _gifDurations.GetValueOrDefault(visualPath, TimeSpan.Zero);

        IsVisible = true;
        ImageElement.Opacity = 1;
        await Task.Delay(150); // <-- Opacity transition duration

        ImageBehavior.SetSpeedRatio(ImageElement, speedRatio);

        // Play sound, slaved to animation start to reduce desync
        using var audioStream = AssetLoader.Open(new Uri($"avares://Peep.Avalonia/{audioPath}"));
        var audioMs = new MemoryStream();
        await audioStream.CopyToAsync(audioMs);
        audioMs.Position = 0;

        _audioReader = new WaveFileReader(audioMs);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_audioReader);
        _waveOut.Play();

        // Wait for GIF to finish
        await Task.Delay(visualDuration);

        ImageBehavior.SetSpeedRatio(ImageElement, 0);
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
