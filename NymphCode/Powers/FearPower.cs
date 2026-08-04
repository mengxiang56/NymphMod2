using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class FearPower : ModPowerTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>()
    ];

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/FearPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/FearPower84.png");

    public override LocString Description =>
        new LocString(
            "powers",
            IsCanonical
                ? "NYMPH_POWER_FEAR_POWER.description"
                : Owner?.IsPlayer == true
                    ? "NYMPH_POWER_FEAR_POWER.descriptionOnPlayer"
                    : "NYMPH_POWER_FEAR_POWER.descriptionOnEnemy");

    protected override string SmartDescriptionLocKey =>
        !IsCanonical && Owner?.IsPlayer == true
            ? "NYMPH_POWER_FEAR_POWER.smartDescriptionOnPlayer"
            : "NYMPH_POWER_FEAR_POWER.smartDescriptionOnEnemy";

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
