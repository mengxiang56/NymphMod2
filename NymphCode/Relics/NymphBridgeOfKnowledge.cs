using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphBridgeOfKnowledge : ModRelicTemplate
{
    private const int BlockAmount = 6;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override RelicAssetProfile AssetProfile =>
        NymphRelicAssets.FromFile("知识之桥");

    internal static async Task TryGainBlockFromConceive(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? source)
    {
        if (source is null
            || !source.Keywords.Contains(NymphKeywords.Conceive)
            || player.Creature.HasPower<BridgeOfKnowledgeUsedThisTurnPower>())
        {
            return;
        }

        NymphBridgeOfKnowledge? relic = player.Relics
            .OfType<NymphBridgeOfKnowledge>()
            .FirstOrDefault();
        if (relic is null)
        {
            return;
        }

        relic.Flash();
        await PowerCmd.Apply<BridgeOfKnowledgeUsedThisTurnPower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            source,
            silent: true);
        await CreatureCmd.GainBlock(
            player.Creature,
            BlockAmount,
            ValueProp.Unpowered,
            null);
    }
}
