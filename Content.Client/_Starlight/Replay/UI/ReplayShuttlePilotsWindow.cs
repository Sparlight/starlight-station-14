using System.Linq;
using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Starlight.Replay.UI;

/// <summary>
/// A shuttle's pilots per console, with held keys. Updated by <see cref="ReplayObserverSystem"/>.
/// </summary>
public sealed class ReplayShuttlePilotsWindow : DefaultWindow
{
    private static readonly Color _litColor = Color.FromHex("#3e9c4f");
    private static readonly Color _unlitColor = Color.FromHex("#26262b");

    // Key pad layout, relative to the pilot's console.
    private static readonly (ShuttleButtons Button, string Text)[][] _padRows =
    [
        [
            (ShuttleButtons.RotateLeft, "replay-observer-shuttle-key-ccw"),
            (ShuttleButtons.StrafeUp, "replay-observer-shuttle-key-up"),
            (ShuttleButtons.RotateRight, "replay-observer-shuttle-key-cw"),
        ],
        [
            (ShuttleButtons.StrafeLeft, "replay-observer-shuttle-key-left"),
            (ShuttleButtons.StrafeDown, "replay-observer-shuttle-key-down"),
            (ShuttleButtons.StrafeRight, "replay-observer-shuttle-key-right"),
        ],
        [
            (ShuttleButtons.Brake, "replay-observer-shuttle-key-brake"),
        ],
    ];

    private readonly OptionButton _shuttles = new() { HorizontalExpand = true };
    private readonly Label _empty = new() { FontColorOverride = Color.Gray };
    private readonly BoxContainer _consoles = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 10 };
    private readonly Label _motion = new();

    private readonly List<EntityUid> _shuttleIds = new();
    private readonly Dictionary<EntityUid, List<(ShuttleButtons Button, StyleBoxFlat Box)>> _pads = new();
    private string? _layoutKey;

    public event Action<EntityUid>? OnShuttleSelected;
    public event Action<EntityUid>? OnPilotPressed;

    public ReplayShuttlePilotsWindow()
    {
        Title = Loc.GetString("replay-observer-shuttle-pilots-title");
        MinSize = new Vector2(300, 220);
        SetSize = new Vector2(340, 420);

        _shuttles.OnItemSelected += args =>
        {
            _shuttles.SelectId(args.Id);
            if (args.Id >= 0 && args.Id < _shuttleIds.Count)
                OnShuttleSelected?.Invoke(_shuttleIds[args.Id]);
        };

        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Children =
            {
                _shuttles,
                new ScrollContainer
                {
                    HScrollEnabled = false,
                    VerticalExpand = true,
                    Children =
                    {
                        new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Vertical,
                            Children = { _empty, _consoles },
                        },
                    },
                },
                _motion,
            },
        });
    }

    public void Update(ShuttlePilotsView view)
    {
        // Rebuild only when pilots change; key presses just recolor.
        var layoutKey = LayoutKey(view);
        if (layoutKey != _layoutKey)
        {
            _layoutKey = layoutKey;
            Rebuild(view);
        }

        foreach (var console in view.Consoles)
        {
            foreach (var pilot in console.Pilots)
            {
                if (!_pads.TryGetValue(pilot.Pilot, out var pad))
                    continue;

                foreach (var (button, box) in pad)
                {
                    box.BackgroundColor = (pilot.Buttons & button) != 0 ? _litColor : _unlitColor;
                }
            }
        }

        _motion.Text = view.Motion ?? string.Empty;
    }

    private void Rebuild(ShuttlePilotsView view)
    {
        _shuttleIds.Clear();
        _shuttles.Clear();
        for (var i = 0; i < view.Shuttles.Count; i++)
        {
            _shuttleIds.Add(view.Shuttles[i].Shuttle);
            _shuttles.AddItem(view.Shuttles[i].Name, i);
        }

        var selected = view.Selected is { } sel ? _shuttleIds.IndexOf(sel) : -1;
        if (selected >= 0)
            _shuttles.SelectId(selected);

        _shuttles.Disabled = _shuttleIds.Count == 0;

        _pads.Clear();
        _consoles.RemoveAllChildren();

        _empty.Text = view.Shuttles.Count == 0
            ? Loc.GetString("replay-observer-shuttle-pilots-none")
            : Loc.GetString("replay-observer-shuttle-pilots-none-here");
        _empty.Visible = view.Consoles.Count == 0;

        foreach (var console in view.Consoles)
        {
            var group = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 4 };
            group.AddChild(new Label { Text = console.Title, FontColorOverride = Color.LightSteelBlue, ClipText = true });

            foreach (var pilot in console.Pilots)
            {
                group.AddChild(BuildPilotRow(pilot));
            }

            _consoles.AddChild(group);
        }
    }

    private Control BuildPilotRow(PilotView pilot)
    {
        var name = new Button
        {
            Text = pilot.Name,
            ToolTip = Loc.GetString("replay-observer-shuttle-pilot-follow"),
            ClipText = true,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };
        name.OnPressed += _ => OnPilotPressed?.Invoke(pilot.Pilot);

        var pad = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2 };
        var boxes = new List<(ShuttleButtons, StyleBoxFlat)>();
        foreach (var row in _padRows)
        {
            var line = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 2 };
            foreach (var (button, loc) in row)
            {
                var box = new StyleBoxFlat { BackgroundColor = _unlitColor };
                boxes.Add((button, box));
                line.AddChild(new PanelContainer
                {
                    PanelOverride = box,
                    MinSize = new Vector2(row.Length == 1 ? 106 : 34, 22),
                    Children = { new Label { Text = Loc.GetString(loc), Align = Label.AlignMode.Center } },
                });
            }

            pad.AddChild(line);
        }

        _pads[pilot.Pilot] = boxes;

        return new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            Children = { name, pad },
        };
    }

    private static string LayoutKey(ShuttlePilotsView view) =>
        string.Join("|", view.Shuttles.Select(s => $"{s.Shuttle}:{s.Name}"))
        + $"#{view.Selected}#"
        + string.Join("|", view.Consoles.Select(c => $"{c.Console}:{c.Title}:{string.Join(",", c.Pilots.Select(p => $"{p.Pilot}:{p.Name}"))}"));
}

public sealed record ShuttlePilotsView(
    List<(EntityUid Shuttle, string Name)> Shuttles,
    EntityUid? Selected,
    List<ConsoleView> Consoles,
    string? Motion);

public sealed record ConsoleView(EntityUid Console, string Title, List<PilotView> Pilots);

public sealed record PilotView(EntityUid Pilot, string Name, ShuttleButtons Buttons);
