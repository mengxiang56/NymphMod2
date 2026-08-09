using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;
using Nymph.Enchantments;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphCityOfTransformation : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.RecreateId),
        .. HoverTipFactory.FromEnchantment<NymphTransformationEnchantment>(1)
    ];

    public NymphCityOfTransformation()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        EnchantmentModel enchantment =
            ModelDb.Enchantment<NymphTransformationEnchantment>();

        foreach (CardModel card in PileType.Hand
                     .GetPile(Owner)
                     .Cards
                     .ToList())
        {
            card.EnergyCost.SetUntilPlayed(0);
            if (card.Enchantment
                    is NymphTransformationEnchantment)
            {
                continue;
            }

            if (enchantment.CanEnchant(card))
            {
                CardCmd.Enchant<NymphTransformationEnchantment>(
                    card,
                    1);
            }
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
