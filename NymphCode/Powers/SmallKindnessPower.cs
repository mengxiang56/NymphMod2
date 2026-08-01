using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class SmallKindnessPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/KindnessPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/KindnessPower84.png");

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player
            || ThoughtMechanics.NarratedAmount(cardPlay) <= 0)
        {
            return;
        }

        List<NecrosisPower> necrosisPowers = CombatState
            .GetOpponentsOf(Owner)
            .Where(enemy => !enemy.IsDead)
            .Select(enemy => enemy.GetPower<NecrosisPower>())
            .Where(power => power is not null)
            .Cast<NecrosisPower>()
            .ToList();
        if (necrosisPowers.Count == 0)
        {
            return;
        }

        Flash();
        foreach (NecrosisPower necrosis in necrosisPowers)
        {
            await necrosis.Trigger(
                choiceContext,
                Owner,
                cardPlay.Card,
                Amount,
                advancesReduction: false);
        }
    }
}
