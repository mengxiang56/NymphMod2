using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.SecondaryResources;

namespace Nymph.Mechanics;

public sealed partial class NThoughtCounter : NSecondaryResourceCounter
{
    private MegaLabel? _amountLabel;
    private MegaLabel? _thresholdLabel;

    public static int AmountFontSize { get; set; } =
        ThoughtMechanics.AmountFontSize;

    public static int ThresholdFontSize { get; set; } =
        ThoughtMechanics.ThresholdFontSize;

    public static float AmountLabelVerticalOffset { get; set; } =
        ThoughtMechanics.AmountLabelVerticalOffset;

    public static float ThresholdLabelOffsetY { get; set; } =
        ThoughtMechanics.ThresholdLabelOffsetY;

    private Vector2? _amountLabelBasePosition;
    private Player? _player;

    public override void _Ready()
    {
        base._Ready();
        SetupLabels();
    }

    private void SetupLabels()
    {
        _amountLabel ??= FindAmountLabel();
        if (_amountLabel is null)
        {
            return;
        }

        CreateThresholdLabelIfNeeded();
        ApplyLabelLayout();
    }

    private MegaLabel? FindAmountLabel()
    {
        foreach (Node child in GetChildren())
        {
            if (child is MegaLabel label)
            {
                return label;
            }
        }

        return null;
    }

    private void CreateThresholdLabelIfNeeded()
    {
        if (_thresholdLabel is not null || _amountLabel is null)
        {
            return;
        }

        _thresholdLabel = new MegaLabel
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = _amountLabel.CustomMinimumSize,
            Size = _amountLabel.Size,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutoSizeEnabled = false,
        };
        AddChild(_thresholdLabel);
    }

    private void ApplyLabelLayout()
    {
        if (_amountLabel is null)
        {
            return;
        }

        _amountLabel.MaxFontSize = AmountFontSize;
        _amountLabel.MinFontSize = Mathf.Max(8, AmountFontSize - 6);

        _amountLabelBasePosition ??= _amountLabel.Position;
        Vector2 amountPosition = _amountLabelBasePosition.Value
            + new Vector2(0f, AmountLabelVerticalOffset);
        _amountLabel.Position = amountPosition;

        if (_thresholdLabel is null)
        {
            return;
        }

        ApplyThresholdTheme();
        _thresholdLabel.Position = amountPosition + new Vector2(0f, ThresholdLabelOffsetY);
    }

    private void ApplyThresholdTheme()
    {
        if (_amountLabel is null || _thresholdLabel is null)
        {
            return;
        }

        Font? font = _amountLabel.GetThemeFont(ThemeConstants.Label.Font);
        if (font is not null)
        {
            _thresholdLabel.AddThemeFontOverride(ThemeConstants.Label.Font, font);
        }

        _thresholdLabel.AddThemeFontSizeOverride(
            ThemeConstants.Label.FontSize,
            ThresholdFontSize);
        _thresholdLabel.AddThemeColorOverride(
            ThemeConstants.Label.FontColor,
            _amountLabel.GetThemeColor(ThemeConstants.Label.FontColor));
        _thresholdLabel.AddThemeColorOverride(
            ThemeConstants.Label.FontOutlineColor,
            _amountLabel.GetThemeColor(ThemeConstants.Label.FontOutlineColor));
        _thresholdLabel.AddThemeConstantOverride(
            ThemeConstants.Label.OutlineSize,
            _amountLabel.GetThemeConstant(ThemeConstants.Label.OutlineSize));
    }

    private void UpdateThresholdLabel(int? threshold, bool visible)
    {
        if (_thresholdLabel is null)
        {
            return;
        }

        _thresholdLabel.Visible = visible && threshold is not null;
        if (!(_thresholdLabel.Visible && threshold is { } value))
        {
            return;
        }

        _thresholdLabel.Text = ThoughtMechanics.FormatThresholdLine(value);
    }

    public void BindThoughtPlayer(Player? player)
    {
        _player = player;
        AutoRefresh = false;
        Visible = player is not null;
        if (player is null)
        {
            SetAmount(0);
            UpdateThresholdLabel(null, visible: false);
            return;
        }

        int threshold = ThoughtMechanics.GetConfusedThreshold(player);
        SetAmount(ThoughtMechanics.Get(player), threshold);
        SetupLabels();
        UpdateThresholdLabel(threshold, visible: true);
    }

    public override void _Process(double delta)
    {
        if (_player is not null)
        {
            ThoughtMechanics.UpdateCounter(this, _player);
        }

        base._Process(delta);
    }
}
