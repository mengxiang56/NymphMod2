using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class HeartlashCommunionPower : ModPowerTemplate
{
    private Creature? _protectedFrom;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/HeartlashCommunionPower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/HeartlashCommunionPower84.png");

    public override LocString Description
    {
        get
        {
            LocString description = new(
                "powers",
                "NYMPH_POWER_HEARTLASH_COMMUNION_POWER.description");
            description.Add(
                "TargetName",
                ProtectedFrom?.Name ?? new LocString(
                    "powers",
                    "NYMPH_POWER_HEARTLASH_COMMUNION_POWER.unknownTarget")
                    .GetFormattedText());
            return description;
        }
    }

    [SavedProperty]
    public Creature? ProtectedFrom
    {
        get => _protectedFrom;
        set
        {
            AssertMutable();
            _protectedFrom = value;
        }
    }

    public override decimal ModifyDamageCap(
        Creature? target,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return target == Owner && dealer == ProtectedFrom
            ? 0
            : decimal.MaxValue;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (ProtectedFrom is null || side == ProtectedFrom.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
