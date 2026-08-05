using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class KeyToHeartPower : ModPowerTemplate
{
    private bool IsOnEnemy => Owner?.Player is null;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        IsOnEnemy
            ? [HoverTipFactory.FromPower<StrengthPower>()]
            : [HoverTipFactory.FromPower<NecrosisPower>()];

    public override PowerType Type => IsOnEnemy ? PowerType.Debuff : PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/KeyToHeartPower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/KeyToHeartPower84.png");

    public override LocString Description => new(
        "powers",
        IsOnEnemy
            ? "NYMPH_POWER_KEY_TO_HEART_STRENGTH_DOWN_POWER.description"
            : "NYMPH_POWER_KEY_TO_HEART_POWER.description");

    protected override string SmartDescriptionLocKey =>
        IsOnEnemy
            ? "NYMPH_POWER_KEY_TO_HEART_STRENGTH_DOWN_POWER.smartDescription"
            : "NYMPH_POWER_KEY_TO_HEART_POWER.smartDescription";

    public override async Task BeforeApplied(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (target.Player is not null)
        {
            return;
        }

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            target,
            -amount,
            applier,
            cardSource,
            silent: true);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Owner.Player is not null
            || amount == Amount
            || power != this)
        {
            return;
        }

        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            -amount,
            applier,
            cardSource,
            silent: true);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner.Player is not null || !participants.Contains(Owner))
        {
            return;
        }

        Flash();
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            Amount,
            Owner,
            null);
    }
}
