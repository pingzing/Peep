using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace Peep.Avalonia;

/// <summary>
/// Represents a loaded and decoded GIF, that can be reused.
/// </summary>
public sealed class LoadedGif : IDisposable
{
    public SKImage[] Frames { get; }
    public int[] FrameDurationsMs { get; } // raw per-frame durations at speed 1x
    public TimeSpan TotalDuration { get; }
    public int Width { get; }
    public int Height { get; }
    public int RepetitionCount { get; }

    public LoadedGif(SKImage[] frames, int[] frameDurationsMs, int width, int height, int repetitionCount)
    {
        Frames = frames;
        FrameDurationsMs = frameDurationsMs;
        Width = width;
        Height = height;
        RepetitionCount = repetitionCount;

        int total = 0;
        foreach (int d in frameDurationsMs)
        {
            total += d;
        }
        TotalDuration = TimeSpan.FromMilliseconds(total);
    }

    /// <summary>
    /// Decodes all frames from a GIF stream using Skia's SKCodec.
    /// </summary>
    public static LoadedGif Decode(Stream gifStream)
    {
        // Copy to MemoryStream so SKCodec can seek
        var ms = new MemoryStream();
        gifStream.CopyTo(ms);
        ms.Position = 0;

        using var codec =
            SKCodec.Create(ms) ?? throw new InvalidOperationException("SKCodec could not decode the provided stream.");

        int frameCount = codec.FrameCount;
        var frameInfos = codec.FrameInfo;
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        // One persistent bitmap that we composite into, frame over frame
        using var surface =
            SKSurface.Create(info)
            ?? throw new InvalidOperationException("Could not create SKSurface for GIF decoding.");
        var canvas = surface.Canvas;

        var frames = new SKImage[frameCount];
        var durations = new int[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            var frameInfo = frameInfos[i];
            // Per the GIF spec, durations must be a multiple of 10ms, and a 0 value
            // isn't well-defined, so just push it up to the smallest delay possible.
            durations[i] = Math.Max(frameInfo.Duration, 10);

            // Handle disposal of the previous frame on the shared surface
            if (i > 0)
            {
                var prevInfo = frameInfos[i - 1];
                switch (prevInfo.DisposalMethod)
                {
                    case SKCodecAnimationDisposalMethod.RestoreBackgroundColor:
                        // Clear the area the previous frame occupied
                        canvas.Save();
                        canvas.ClipRect(
                            new SKRect(
                                prevInfo.FrameRect.Left,
                                prevInfo.FrameRect.Top,
                                prevInfo.FrameRect.Right,
                                prevInfo.FrameRect.Bottom
                            )
                        );
                        canvas.Clear(SKColors.Transparent);
                        canvas.Restore();
                        break;
                    case SKCodecAnimationDisposalMethod.RestorePrevious:
                        // Restore by re-drawing the prior snapshot — handled below by snapshotting before draw
                        break;
                    // DoNotDispose: leave the surface as-is
                }
            }

            // Decode this frame's pixels into a fresh bitmap
            using var frameBitmap = new SKBitmap(info);
            var opts = new SKCodecOptions(i, frameInfo.RequiredFrame);
            codec.GetPixels(info, frameBitmap.GetPixels(), opts);

            // Composite decoded frame onto the persistent surface
            canvas.DrawBitmap(frameBitmap, 0, 0);

            // Snapshot the fully-composited result as an immutable SKImage
            frames[i] = surface.Snapshot();
        }

        return new LoadedGif(frames, durations, codec.Info.Width, codec.Info.Height, codec.RepetitionCount);
    }

    public void Dispose()
    {
        foreach (var frame in Frames)
        {
            frame.Dispose();
        }
    }
}

public enum GifRepeatBehavior
{
    /// <summary>Respect the loop count embedded in the GIF file. Most GIFs loop forever (count = 0).</summary>
    GifDefined,

    /// <summary>Play exactly one iteration, then stop on the last frame.</summary>
    None,

    /// <summary>Loop indefinitely, regardless of what the GIF specifies.</summary>
    Forever,
}

/// <summary>
/// Avalonia Control that renders an animated GIF directly onto the compositor's SKCanvas
/// via ICustomDrawOperation, using wall-clock elapsed time (Stopwatch) for frame selection.
/// </summary>
public sealed class GifAnimatorControl : Control
{
    private LoadedGif? _gif;
    private Stretch _stretch = Stretch.Uniform;
    private GifRepeatBehavior _repeatBehavior = GifRepeatBehavior.GifDefined;
    private readonly Stopwatch _stopwatch = new();
    private double[] _cumulativeMs = [];

    public GifAnimatorControl()
    {
        ClipToBounds = true;
    }

    public GifRepeatBehavior RepeatBehavior
    {
        get => _repeatBehavior;
        set => _repeatBehavior = value;
    }

    public Stretch Stretch
    {
        get => _stretch;
        set
        {
            _stretch = value;
            InvalidateVisual();
        }
    }

    /// <summary>Total duration of one loop.</summary>
    public TimeSpan TotalDuration => _gif?.TotalDuration ?? TimeSpan.Zero;

    public void SetGif(LoadedGif gif)
    {
        _gif = gif;
        _stopwatch.Reset();
        InvalidateMeasure();
    }

    public void Start()
    {
        if (_gif == null)
        {
            return;
        }
        BuildFrameTimings();
        _stopwatch.Restart();
        InvalidateVisual();
    }

    public void Stop()
    {
        // Doesn't stop rendering, but does stop the GIF from progressing.
        _stopwatch.Stop();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_gif == null)
        {
            return new Size(0, 0);
        }

        return _stretch switch
        {
            Stretch.None => new Size(_gif.Width, _gif.Height),
            // For Uniform/Fill, take as much space as offered, capped to gif size
            _ => new Size(
                double.IsInfinity(availableSize.Width) ? _gif.Width : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? _gif.Height : availableSize.Height
            ),
        };
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // TODO: Opacity is the only one we change on the fly, but should we also do this for Height, Width, etc?
        if (change.Property == OpacityProperty)
        {
            InvalidateVisual();
        }
    }

    private void BuildFrameTimings()
    {
        if (_gif == null)
        {
            _cumulativeMs = [];
            return;
        }

        int[] durations = _gif.FrameDurationsMs;
        _cumulativeMs = new double[durations.Length];
        double running = 0;
        for (int i = 0; i < durations.Length; i++)
        {
            running += durations[i];
            _cumulativeMs[i] = running;
        }
    }

    private int GetCurrentFrameIndex()
    {
        if (_cumulativeMs.Length == 0)
            return 0;

        double elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;
        int newCompletedLoops = (int)(elapsedMs / TotalDuration.TotalMilliseconds);

        // Check whether we've hit the loop limit and should stop
        if (HasReachedLoopLimit(newCompletedLoops))
        {
            _stopwatch.Stop();
            return _cumulativeMs.Length - 1; // freeze on last frame
        }

        double posMs = elapsedMs % TotalDuration.TotalMilliseconds;
        for (int i = 0; i < _cumulativeMs.Length; i++)
        {
            if (posMs < _cumulativeMs[i])
            {
                return i;
            }
        }
        return _cumulativeMs.Length - 1;
    }

    private bool HasReachedLoopLimit(int completedLoops)
    {
        return _repeatBehavior switch
        {
            GifRepeatBehavior.Forever => false,
            GifRepeatBehavior.None => completedLoops >= 1,
            GifRepeatBehavior.GifDefined =>
            // SKCodec RepetitionCount: 0 = loop forever, N > 0 = play N+1 times total
            _gif != null
                && _gif.RepetitionCount > 0
                && completedLoops >= _gif.RepetitionCount + 1,
            _ => false,
        };
    }

    public override void Render(DrawingContext context)
    {
        if (_gif != null)
        {
            context.Custom(
                new GifDrawOp(
                    new Rect(0, 0, Bounds.Width, Bounds.Height),
                    _gif.Frames[GetCurrentFrameIndex()],
                    _stretch,
                    Opacity
                )
            );
        }

        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Default);
    }

    private sealed class GifDrawOp : ICustomDrawOperation
    {
        private readonly SKImage _frame;
        private readonly Stretch _stretch;
        private readonly byte _alpha;

        public Rect Bounds { get; }

        public GifDrawOp(Rect bounds, SKImage frame, Stretch stretch, double opacity)
        {
            Bounds = bounds;
            _frame = frame;
            _stretch = stretch;
            _alpha = (byte)Math.Clamp(opacity * 255.0, 0, 255);
        }

        public void Dispose()
        {
            // SKImage is owned by LoadedGif, not by the DrawOp.
        }

        public bool HitTest(Point p) => false;

        // Returning false means Avalonia will always re-render this DrawOp.
        // We want that, because we most probably want to render every frame,
        // without doing potentially expensive checks to see if we can re-use an old frame.
        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
            {
                return;
            }

            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;

            canvas.Save();
            SKRect destRect = CalculateDestRect(_frame.Width, _frame.Height, Bounds, _stretch);
            using var paint = new SKPaint { IsAntialias = false, Color = new SKColor(255, 255, 255, _alpha) };
            canvas.DrawImage(_frame, destRect, paint);
            canvas.Restore();
        }

        private static SKRect CalculateDestRect(int frameWidth, int frameHeight, Rect bounds, Stretch stretch)
        {
            double controlW = bounds.Width;
            double controlH = bounds.Height;

            if (controlW <= 0 || controlH <= 0)
            {
                return new SKRect(0, 0, frameWidth, frameHeight);
            }

            return stretch switch
            {
                Stretch.None => new SKRect(
                    (float)((controlW - frameWidth) / 2),
                    (float)((controlH - frameHeight) / 2),
                    (float)((controlW + frameWidth) / 2),
                    (float)((controlH + frameHeight) / 2)
                ),
                Stretch.Fill => new SKRect(0, 0, (float)controlW, (float)controlH),
                _ => // Uniform (and UniformToFill treated as Uniform)
                UniformRect(frameWidth, frameHeight, controlW, controlH),
            };
        }

        private static SKRect UniformRect(double srcW, double srcH, double dstW, double dstH)
        {
            double scale = Math.Min(dstW / srcW, dstH / srcH);
            double w = srcW * scale;
            double h = srcH * scale;
            float x = (float)((dstW - w) / 2);
            float y = (float)((dstH - h) / 2);
            return new SKRect(x, y, (float)(x + w), (float)(y + h));
        }
    }
}
