using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class ThoughtPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/ui/thought/thought_clear.png",
        BigIconPath: $"{Entry.ResPath}/images/ui/thought/thought_clear.png");

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner
            || !props.IsPoweredAttack()
            || Owner.Player is not { } player)
        {
            return 1m;
        }

        return ThoughtMechanics.GetAttackDamageMultiplier(
            player,
            cardPlay);
    }

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner
            || !props.IsPoweredCardOrMonsterMoveBlock()
            || Owner.Player is not { } player)
        {
            return 1m;
        }

        return ThoughtMechanics.GetCardBlockMultiplier(
            player,
            cardPlay);
    }
}
