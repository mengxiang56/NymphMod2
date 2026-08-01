using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;
using Nymph.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphBlueprintMapping : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<NymphTransformationEnchantment>(1);

    public NymphBlueprintMapping()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        List<CardModel> drawn =
        [
            .. await CardPileCmd.Draw(
                choiceContext,
                DynamicVars.Cards.IntValue,
                Owner)
        ];

        EnchantmentModel enchantment =
            ModelDb.Enchantment<NymphTransformationEnchantment>();
        HashSet<CardModel> candidates = drawn
            .Where(enchantment.CanEnchant)
            .ToHashSet();
        if (candidates.Count == 0)
        {
            return;
        }

        CardSelectorPrefs prefs = new(
            SelectionScreenPrompt,
            0,
            candidates.Count)
        {
            Cancelable = true
        };
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            card => candidates.Contains(card),
            this);

        foreach (CardModel card in selected)
        {
            CardCmd.Enchant<NymphTransformationEnchantment>(card, 1);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
