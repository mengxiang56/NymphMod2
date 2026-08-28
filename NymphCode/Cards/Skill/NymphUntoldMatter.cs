using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphDerivedCardPool))]
public sealed class NymphUntoldMatter : ModCardTemplate, ISelfRecreatingOnPlayCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain,
        NymphKeywords.Conceive,
        NymphKeywords.Recreate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", 4)
    ];

    public NymphUntoldMatter()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
    {
    }

#if STS2_PUBLIC
    protected override PileType GetResultPileTypeForCardPlay()
    {
        return PileType.None;
    }
#else
    protected override CardLocation GetResultLocationForCardPlay()
    {
        return new CardLocation(
            Owner,
            PileType.None,
            CardPilePosition.Bottom);
    }
#endif

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await ThoughtMechanics.Create(
            choiceContext,
            Owner,
            DynamicVars["Create"].IntValue,
            this);
        if (FremontMechanicsPower.IsMarkedForCoffin(this))
        {
            return;
        }

        await RecreateMechanics.CreateReplacementInHand(
            this,
            card => card.CanonicalKeywords.Contains(
                NymphKeywords.Narrate));
    }

    protected override void OnUpgrade()
    {
    }
}
