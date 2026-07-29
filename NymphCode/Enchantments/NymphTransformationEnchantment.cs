using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Enchantments;

[RegisterEnchantment]
public sealed class NymphTransformationEnchantment
    : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/cards/NymphFurnaceIllusion.png");

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        return card == Card
            ? new CardLocation(
                card.Owner,
                PileType.None,
                CardPilePosition.Bottom)
            : cardLocation;
    }

    public override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card)
        {
            return;
        }

        await RecreateMechanics.CreateReplacementInHand(Card);
    }
}
