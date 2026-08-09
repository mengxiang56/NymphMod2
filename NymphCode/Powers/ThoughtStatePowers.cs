using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using Nymph.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

public abstract class ThoughtStatePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected NymphDifficulty Difficulty =>
        NymphDifficultyManager.GetFor(Target?.Player);

    protected static LocString DifficultyDescription(string key) =>
        new("powers", key);
}

[RegisterPower]
public sealed class ThoughtBurdenStrengthPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
}

[RegisterPower]
public sealed class LucidPower : ThoughtStatePower
{
    public override LocString Description =>
        Difficulty == NymphDifficulty.Collapse
            ? DifficultyDescription("NYMPH_POWER_LUCID_POWER.collapseDescription")
            : base.Description;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/LucidPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/LucidPower84.png");
}

[RegisterPower]
public sealed class FracturedPower : ThoughtStatePower
{
    public override LocString Description => Difficulty switch
    {
        NymphDifficulty.Heavy => DifficultyDescription(
            "NYMPH_POWER_FRACTURED_POWER.heavyDescription"),
        NymphDifficulty.Collapse => DifficultyDescription(
            "NYMPH_POWER_FRACTURED_POWER.collapseDescription"),
        _ => base.Description
    };

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/FracturedPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/FracturedPower84.png");
}

[RegisterPower]
public sealed class ObstructedPower : ThoughtStatePower
{
    public override LocString Description => Difficulty switch
    {
        NymphDifficulty.Heavy => DifficultyDescription(
            "NYMPH_POWER_OBSTRUCTED_POWER.heavyDescription"),
        NymphDifficulty.Collapse => DifficultyDescription(
            "NYMPH_POWER_OBSTRUCTED_POWER.collapseDescription"),
        _ => base.Description
    };

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/ObstructedPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/ObstructedPower84.png");
}
