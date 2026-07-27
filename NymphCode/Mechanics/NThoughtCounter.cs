using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.SecondaryResources;

namespace Nymph.Mechanics;

public sealed partial class NThoughtCounter : NSecondaryResourceCounter
{
    private Player? _player;

    public void BindThoughtPlayer(Player? player)
    {
        _player = player;
        AutoRefresh = false;
        Visible = player is not null;
        SetAmount(player is null ? 0 : ThoughtMechanics.Get(player));
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
