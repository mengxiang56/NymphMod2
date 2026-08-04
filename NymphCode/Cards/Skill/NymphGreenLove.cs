using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphGreenLove : ModCardTemplate
{
    public override bool GainsBlock => true;

    protected override bool ShouldGlowGoldInternal =>
        ThoughtMechanics.CanNarrateAll(Owner);

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust,
        NymphKeywords.Narrate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    public NymphGreenLove()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await ThoughtMechanics.NarrateAll(
            choiceContext,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
