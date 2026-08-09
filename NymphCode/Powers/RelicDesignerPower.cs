using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using Nymph.Rewards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class RelicDesignerPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/RelicDesignerPower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/RelicDesignerPower84.png");

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Player is null)
        {
            return Task.CompletedTask;
        }

        for (int i = 0; i < Amount; i++)
        {
            room.AddExtraReward(
                Owner.Player,
                new RecreateRelicReward(Owner.Player));
        }

        return Task.CompletedTask;
    }
}
