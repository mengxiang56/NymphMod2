using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphGatherIdea : ModCardTemplate
{
    public override bool GainsBlock => true;

    protected override bool ShouldGlowGoldInternal =>
        ThoughtMechanics.CanNarrate(
            Owner,
            DynamicVars["Narrate"].IntValue);

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust,
        NymphKeywords.Narrate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar("Narrate", 5)
    ];

    public NymphGatherIdea()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await MoveDiscardToHand(
            choiceContext,
            DynamicVars.Cards.IntValue);

        int narrated = await ThoughtMechanics.Narrate(
            choiceContext,
            cardPlay,
            DynamicVars["Narrate"].IntValue);
        if (narrated <= 0)
        {
            return;
        }

        await MoveDiscardToHand(
            choiceContext,
            DynamicVars.Cards.IntValue
                * ThoughtMechanics.NarrationEffectMultiplier(Owner));
    }

    private async Task MoveDiscardToHand(
        PlayerChoiceContext choiceContext,
        int count)
    {
        if (count <= 0)
        {
            return;
        }

        CardPile discardPile = PileType.Discard.GetPile(Owner);
        if (discardPile.Cards.Count == 0)
        {
            return;
        }

        int maxCount = Math.Min(count, discardPile.Cards.Count);
        CardSelectorPrefs prefs = new(
            SelectionScreenPrompt,
            maxCount,
            maxCount);
        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
            choiceContext,
            discardPile,
            Owner,
            prefs);

        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
    }
}
