using MegaCrit.Sts2.Core.Entities.Relics;
using Nymph.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphDisasterOrigin : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override RelicAssetProfile AssetProfile =>
        NymphRelicAssets.FromFile("灾难之源");
}
