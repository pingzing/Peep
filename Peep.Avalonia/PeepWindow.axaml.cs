using AnimatedImage.Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Peep.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Peep.Avalonia;

public partial class PeepWindow : Window
{
    private readonly List<(string, string, Stretch, TimeSpan, double)> _ventressInfo =
        new()
        {
            (
                "sounds/ventress/peep.mp3",
                "gifs/ventress/peep.gif",
                Stretch.Uniform,
                TimeSpan.FromMilliseconds(1200),
                5.0
            )
        };

    private readonly List<(string, string, Stretch, TimeSpan, double)> _kawKawInfo =
        new()
        {
            (
                "sounds/kawkaw/kawkaw_nyon_1.wav",
                "gifs/kawkaw/kawkaw_nyon.gif",
                Stretch.None,
                TimeSpan.FromMilliseconds(800),
                1.0
            ),
            (
                "sounds/kawkaw/kawkaw_nyon_2.wav",
                "gifs/kawkaw/kawkaw_nyon.gif",
                Stretch.None,
                TimeSpan.FromMilliseconds(800),
                1.0
            ),
            (
                "sounds/kawkaw/kawkaw_lick_1.wav",
                "gifs/kawkaw/kawkaw_lick.gif",
                Stretch.None,
                TimeSpan.FromMilliseconds(1500),
                1.0
            ),
            (
                "sounds/kawkaw/kawkaw_lick_2.wav",
                "gifs/kawkaw/kawkaw_lick.gif",
                Stretch.None,
                TimeSpan.FromMilliseconds(1500),
                1.0
            ),
            (
                "sounds/kawkaw/kawkaw_lick_3.wav",
                "gifs/kawkaw/kawkaw_lick.gif",
                Stretch.None,
                TimeSpan.FromMilliseconds(1500),
                1.0
            ),
        };

    public PeepWindow()
    {
        InitializeComponent();
    }

    private (string AudioPath, string VisualPath, Stretch VisualStretch, TimeSpan VisualDuration, double SpeedRatio) GetPeepInfo(
        ChosenCharacter character
    )
    {
        var targetList = character switch
        {
            ChosenCharacter.Ventress => _ventressInfo,
            ChosenCharacter.KawKaw => _kawKawInfo,
            _ => throw new Exception($"Invalid character: {character}")
        };

        int randomIndex = Random.Shared.Next(0, targetList.Count);
        return targetList[randomIndex];
    }

    public async Task Peep(ChosenCharacter chosenCharacter)
    {
        if (IsVisible)
        {
            return;
        }

        ImageElement.Opacity = 0;

        (string audioPath, string visualPath, Stretch visualStretch, TimeSpan visualDuration, double SpeedRatio) =
            GetPeepInfo(chosenCharacter);
        ImageElement.Stretch = visualStretch;
        ImageBehavior.SetSpeedRatio(ImageElement, 0);
        ImageBehavior.SetAnimatedSource(
            ImageElement,
            AnimatedImageSourceConverter.Convert(Path.Combine("avares://Peep.Avalonia/", visualPath))
        );
        ImageBehavior.SetRepeatBehavior(ImageElement, RepeatBehavior.Forever); // <-- prevent GIF from finishing and freezing on first frame

        // TODO: Center on active monitor

        IsVisible = true;
        ImageElement.Opacity = 1;
        await Task.Delay(150); // <-- Opacity transition duration

        ImageBehavior.SetSpeedRatio(ImageElement, SpeedRatio);

        // TODO: Play sound here. Maybe use NAudio?

        // Wait for GIF to finish
        await Task.Delay(visualDuration);

        ImageBehavior.SetSpeedRatio(ImageElement, 0);
        ImageElement.Opacity = 0;
        await Task.Delay(150); // <-- Opacity transition duration

        // TODO: Stop sound
        IsVisible = false;
    }
}
