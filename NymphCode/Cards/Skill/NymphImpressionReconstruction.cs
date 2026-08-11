using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphImpressionReconstruction : ModCardTemplate
{
    protected override bool HasEnergyCostX => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.RecreateId),
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];

    public NymphImpressionReconstruction()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int count = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        IReadOnlyList<RecreateResult> results =
            await RecreateMechanics.SelectFromHand(
                choiceContext,
                this,
                0,
                count);

        foreach (CardModel replacement in results
            .Select(result => result.Replacement))
        {
            replacement.GiveSingleTurnRetain();
        }

        HashSet<CardModel> candidates = results
            .Select(result => result.Replacement)
            .Where(card => card.Pile?.Type == PileType.Hand)
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
        HashSet<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            candidates.Contains,
            this)).ToHashSet();
        await RecreateMechanics.AutoPlayReplacements(
            choiceContext,
            results
                .Where(result => selected.Contains(result.Replacement))
                .ToList());
    }

    protected override void OnUpgrade()
    {
    }
}
