using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class HeartDefenseCollapsePower : ModPowerTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>(),
        HoverTipFactory.FromPower<FearPower>()
    ];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/HeartDefenseCollapsePower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/HeartDefenseCollapsePower84.png");

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not NecrosisPower
            || amount <= 0
            || applier != Owner
            || !CombatState.GetOpponentsOf(Owner).Contains(power.Owner))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<FearPower>(
            choiceContext,
            power.Owner,
            Amount,
            Owner,
            cardSource);
    }
}
