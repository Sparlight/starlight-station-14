using Content.Shared._Starlight.Actions.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._Starlight.Actions.UI;

/// <summary>
/// The struggle minigame bar: a sweet spot (perfect in the middle, good either
/// side) and a cursor sweeping across it. Purely visual; state is pushed in
/// every frame by <see cref="LatchStatusControl"/>.
/// </summary>
public sealed class LatchStruggleBar : Control
{
    // Room at each end so the bite shake never draws outside the control.
    private const float ShakeMargin = 4f;
    private const float BorderThickness = 2f;
    private const float CursorWidth = 2f;

    private static readonly Color BorderColor = new(0.59f, 0.46f, 0.90f);
    private static readonly Color TrackColor = new(0.12f, 0.14f, 0.16f);
    private static readonly Color GoodColor = new(0.86f, 0.64f, 0.99f);
    private static readonly Color PerfectColor = new(0.97f, 0.91f, 0.98f);
    private static readonly Color CursorColor = Color.White;
    private static readonly Color CursorPerfectColor = new(1f, 0.84f, 0.3f);
    private static readonly Color CursorGoodColor = new(0.55f, 0.95f, 0.55f);
    private static readonly Color CursorMissColor = new(0.95f, 0.3f, 0.3f);
    private static readonly Color PausedColor = new(0.35f, 0.35f, 0.38f);

    public float ZoneCenter;
    public float PerfectWidth;
    public float GoodWidth;
    public float Cursor;
    public LatchStruggleResult Result;
    public bool Paused;

    /// <summary>
    /// Horizontal shake offset, in UI units, clamped to <see cref="ShakeMargin"/>.
    /// </summary>
    public float ShakeOffset;

    public LatchStruggleBar()
    {
        MinHeight = 14;
        HorizontalExpand = true;
        MouseFilter = MouseFilterMode.Ignore;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var margin = ShakeMargin * UIScale;
        var shake = Math.Clamp(ShakeOffset, -ShakeMargin, ShakeMargin) * UIScale;
        var border = MathF.Max(1f, MathF.Round(BorderThickness * UIScale));
        // Draw can run before layout gives the bar a real size.
        var minWidth = (2f * margin) + (2f * border) + 1f;
        var minHeight = (2f * border) + 1f;
        if (PixelWidth < minWidth || PixelHeight < minHeight)
            return;

        var left = margin + shake;
        var right = PixelWidth - margin + shake;
        handle.DrawRect(new UIBox2(left, 0f, right, PixelHeight), Tint(BorderColor));

        var trackLeft = left + border;
        var trackRight = right - border;
        var trackWidth = trackRight - trackLeft;
        if (trackWidth <= 0f)
            return;

        var top = border;
        var bottom = PixelHeight - border;
        handle.DrawRect(new UIBox2(trackLeft, top, trackRight, bottom), Tint(TrackColor));

        float X(float fraction) => trackLeft + Math.Clamp(fraction, 0f, 1f) * trackWidth;

        var halfPerfect = PerfectWidth / 2f;
        handle.DrawRect(new UIBox2(X(ZoneCenter - halfPerfect - GoodWidth), top, X(ZoneCenter + halfPerfect + GoodWidth), bottom), Tint(GoodColor));
        handle.DrawRect(new UIBox2(X(ZoneCenter - halfPerfect), top, X(ZoneCenter + halfPerfect), bottom), Tint(PerfectColor));

        var cursorHalf = MathF.Max(1f, CursorWidth * UIScale) / 2f;
        var cursorX = X(Cursor);
        var cursorColor = Result switch
        {
            LatchStruggleResult.Perfect => CursorPerfectColor,
            LatchStruggleResult.Good => CursorGoodColor,
            LatchStruggleResult.Miss => CursorMissColor,
            _ => CursorColor,
        };
        handle.DrawRect(new UIBox2(cursorX - cursorHalf, 0f, cursorX + cursorHalf, PixelHeight), Tint(cursorColor));
    }

    /// <summary>
    /// Greys everything out while struggling is paused.
    /// </summary>
    private Color Tint(Color color) => Paused ? Color.InterpolateBetween(color, PausedColor, 0.75f) : color;
}
