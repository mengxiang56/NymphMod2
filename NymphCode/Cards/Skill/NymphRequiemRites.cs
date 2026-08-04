using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphRequiemRites : ModCardTemplate
{
    public override bool GainsBlock => true;

    protected override bool ShouldGlowGoldInternal =>
        ThoughtMechanics.CanNarrate(
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
        new DynamicVar("Narrate", 6),
        new PowerVar<WeakPower>(1),
        new PowerVar<VulnerablePower>(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public NymphRequiemRites()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Weak.IntValue,
            Owner.Creature,
            this);

        int narrated = await ThoughtMechanics.Narrate(
            choiceContext,
            cardPlay,
            DynamicVars["Narrate"].IntValue);
        if (narrated <= 0)
        {
            return;
        }

        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Vulnerable.IntValue
                * ThoughtMechanics.NarrationEffectMultiplier(Owner),
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(1);
        DynamicVars.Vulnerable.UpgradeValueBy(1);
    }
}
