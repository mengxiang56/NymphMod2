using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class ImaginedEntityPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/cards/NymphImaginedEntity.png",
        BigIconPath: $"{Entry.ResPath}/images/cards/NymphImaginedEntity.png");

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int narrated = ThoughtMechanics.NarratedAmount(cardPlay);
        if (cardPlay.Card.Owner != Owner.Player || narrated <= 0)
        {
            return;
        }

        var target = Owner.Player.RunState.Rng.CombatTargets.NextItem(
            CombatState.GetOpponentsOf(Owner)
                .Where(enemy => !enemy.IsDead));
        if (target is null)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            target,
            narrated * Amount,
            DamageProps.nonCardUnpowered,
            Owner);
    }
}
