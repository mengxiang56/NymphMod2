using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class SanctifierAutomatonSelfDestructPower : ModPowerTemplate
{
    private bool _exploding;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/HolyCityEmbracePower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/HolyCityEmbracePower84.png");

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_exploding
            || Owner.IsDead
            || Owner.Monster is not SanctifierAutomaton automaton
            || power is not BodrakastiDistancePower distance
            || power.Owner.Player is null
            || power.Owner.IsDead
            || distance.Amount != automaton.Distance)
        {
            return;
        }

        _exploding = true;
        await CreatureCmd.Damage(
            choiceContext,
            power.Owner,
            15,
            ValueProp.Unpowered | ValueProp.Move,
            Owner);
        await CreatureCmd.Kill(Owner, force: true);
    }
}
