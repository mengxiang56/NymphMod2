using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphSongOfResilientSoul : ModRelicTemplate
{
    private const int MaxHpGain = 3;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override RelicAssetProfile AssetProfile =>
        NymphRelicAssets.FromFile("神魂坚韧之歌");

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner
            || !cardPlay.Card.Keywords.Contains(NymphKeywords.InspirationCard))
        {
            return;
        }

        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpGain);
    }
}
