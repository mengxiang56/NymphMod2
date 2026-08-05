using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class NecrosisDecayIntervalPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/NecrosisDecayIntervalPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/NecrosisDecayIntervalPower84.png");

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncToNecrosis(Owner);
        DisplayAmountChanged += OnDisplayAmountChanged;
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        DisplayAmountChanged -= OnDisplayAmountChanged;
        SyncToNecrosis(oldOwner);
        return Task.CompletedTask;
    }

    private void OnDisplayAmountChanged()
    {
        SyncToNecrosis(Owner);
    }

    internal static void SyncToNecrosis(Creature owner)
    {
        if (owner.GetPower<NecrosisPower>() is not { } necrosis)
        {
            return;
        }

        int intervalBonus =
            owner.GetPower<NecrosisDecayIntervalPower>()?.Amount ?? 0;
        necrosis.CardsPerLayerLoss =
            NecrosisPower.DefaultCardsPerLayerLoss + intervalBonus;
    }
}
