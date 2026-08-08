using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphScalesOfAvarice : ModRelicTemplate
{
    private const int GoldGain = 15;
    private const int InspirationThreshold = 3;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override RelicAssetProfile AssetProfile =>
        NymphRelicAssets.FromFile("贪婪天平");

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            return;
        }

        if (InspirationMechanics.PileType
                .GetPile(Owner)
                .Cards
                .Count
            < InspirationThreshold)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainGold(GoldGain, Owner);
    }
}
