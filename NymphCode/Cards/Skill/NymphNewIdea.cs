using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphNewIdea : ModCardTemplate
{
    public override bool GainsBlock => true;

    protected override bool ShouldGlowGoldInternal =>
        ThoughtMechanics.CanNarrate(
            Owner,
            DynamicVars["Narrate"].IntValue);

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.Recreate,
        NymphKeywords.Narrate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Recreate", 1),
        new DynamicVar("Narrate", 6)
    ];

    public NymphNewIdea()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int narrated = await ThoughtMechanics.Narrate(
            choiceContext,
            cardPlay,
            DynamicVars["Narrate"].IntValue);

        if (CurrentUpgradeLevel > 0)
        {
            await RecreateMechanics.SelectFromHand(
                choiceContext,
                this,
                0,
                DynamicVars["Recreate"].IntValue,
                makeFreeUntilPlayed: narrated > 0);
            return;
        }

        await RecreateMechanics.RandomFromHand(
            this,
            DynamicVars["Recreate"].IntValue,
            makeFreeUntilPlayed: narrated > 0);
    }

    protected override void OnUpgrade()
    {
    }
}
