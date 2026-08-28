using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Nymph.Mechanics;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Enchantments;

[RegisterEnchantment]
public sealed class NymphTransformationEnchantment
    : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;

    public override bool CanEnchant(CardModel card)
    {
        if (!CanEnchantCardType(card.Type))
        {
            return false;
        }

        CardPile? pile = card.Pile;
        if (pile != null
            && pile.Type == PileType.Deck
            && card.Keywords.Contains(CardKeyword.Unplayable))
        {
            return false;
        }

        if (card.Enchantment != null
            && (!IsStackable || card.Enchantment.GetType() != GetType()))
        {
            return false;
        }

        return true;
    }

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/enchantments/NymphTransformationEnchantment.png");

#if STS2_PUBLIC
    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        return card == Card
            ? (PileType.None, CardPilePosition.Bottom)
            : (pileType, position);
    }
#else
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
#endif

    public override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card
            || Card is ISelfRecreatingOnPlayCard
            || FremontMechanicsPower.IsMarkedForCoffin(Card))
        {
            return;
        }

        await RecreateMechanics.CreateReplacementInHand(Card);
    }
}
