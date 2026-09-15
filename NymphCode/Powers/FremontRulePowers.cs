using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

public abstract class FremontRulePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
}

[RegisterPower]
public sealed class FremontCardChannelRulePower : FremontRulePower
{
    private int _cardsRemaining = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardsRemaining", 5)
    ];

    [SavedProperty]
    public int CardsRemaining
    {
        get => _cardsRemaining;
        set
        {
            AssertMutable();
            _cardsRemaining = Math.Clamp(value, 1, 5);
            DynamicVars["CardsRemaining"].BaseValue = _cardsRemaining;
        }
    }

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CreateHistoryPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CreateHistoryPower84.png");
}

[RegisterPower]
public sealed class FremontBlackCoffinRulePower : FremontRulePower
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/DreadkazEchoPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/DreadkazEchoPower84.png");
}

[RegisterPower]
public sealed class FremontSecondPhaseRulePower : FremontRulePower
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/LookForFuturePower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/LookForFuturePower84.png");
}
