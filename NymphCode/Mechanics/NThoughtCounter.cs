using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.SecondaryResources;

namespace Nymph.Mechanics;

public sealed partial class NThoughtCounter : NSecondaryResourceCounter
{
    /// <summary>
    /// 数值标签两行之间的行距（Label theme constant <c>line_spacing</c>）。
    /// </summary>
    public static int AmountLineSpacing { get; set; } =
        ThoughtMechanics.AmountLineSpacing;

    private Player? _player;

    public override void _Ready()
    {
        base._Ready();
        ApplyAmountLineSpacing();
    }

    private void ApplyAmountLineSpacing()
    {
        foreach (Node child in GetChildren())
        {
            if (child is not MegaLabel label)
            {
                continue;
            }

            label.AddThemeConstantOverride(
                ThemeConstants.Label.LineSpacing,
                AmountLineSpacing);
            break;
        }
    }

    public void BindThoughtPlayer(Player? player)
    {
        _player = player;
        AutoRefresh = false;
        Visible = player is not null;
        if (player is null)
        {
            SetAmount(0);
            return;
        }

        // max 参数传临界点；FormatAmount 会显示为「思绪\n临界点/阻滞点」
        SetAmount(
            ThoughtMechanics.Get(player),
            ThoughtMechanics.GetConfusedThreshold(player));
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
