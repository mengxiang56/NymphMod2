using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphStoryToBeTold : ModCardTemplate
{
    protected override bool ShouldGlowGoldInternal =>
        ThoughtMechanics.WasPreviousCardConceive(this)
        && ThoughtMechanics.CanNarrate(
            Owner,
            DynamicVars["Narrate"].IntValue);

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
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

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ThoughtMechanics.CreateHoverTip(),
        ModKeywordRegistry.CreateHoverTip(
            NymphKeywords.ConceiveId)
    ];

    public NymphStoryToBeTold()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        bool previousCardWasConceive =
            ThoughtMechanics.WasPreviousCardConceive(this);

        await CardPileCmd.Draw(
            choiceContext,
            DynamicVars.Cards.IntValue,
            Owner);

        if (!previousCardWasConceive)
        {
            return;
        }

        int narrated = await ThoughtMechanics.Narrate(
            choiceContext,
            cardPlay,
            DynamicVars["Narrate"].IntValue);
        if (narrated > 0)
        {
            await CardPileCmd.Draw(
                choiceContext,
                DynamicVars.Cards.IntValue,
                Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
