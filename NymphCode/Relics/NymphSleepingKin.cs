using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphSleepingKin : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override RelicAssetProfile AssetProfile =>
        NymphRelicAssets.FromFile("沉眠同胞");

    public override async Task AfterObtained()
    {
        List<RelicModel> eligibleRelics = Owner.Relics
            .Where(relic =>
                relic != this
                && relic.Rarity is not (RelicRarity.Ancient or RelicRarity.Starter))
            .ToList();
        if (eligibleRelics.Count == 0)
        {
            return;
        }

        RelicModel? selected = await RelicSelectCmd.FromChooseARelicScreen(
            Owner,
            eligibleRelics);
        if (selected is null)
        {
            return;
        }

        RelicModel replacement = ModelDb.GetById<RelicModel>(selected.Id).ToMutable();
        await RelicCmd.Replace(this, replacement);
    }
}
