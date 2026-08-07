using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

public abstract class ThoughtStatePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

[RegisterPower]
public sealed class LucidPower : ThoughtStatePower
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/LucidPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/LucidPower84.png");
}

[RegisterPower]
public sealed class FracturedPower : ThoughtStatePower
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/FracturedPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/FracturedPower84.png");
}

[RegisterPower]
public sealed class ObstructedPower : ThoughtStatePower
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/ObstructedPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/ObstructedPower84.png");
}
