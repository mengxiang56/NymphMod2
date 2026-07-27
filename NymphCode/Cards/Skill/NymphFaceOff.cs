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
public sealed class NymphFaceOff : ModCardTemplate
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
        new PowerVar<StrengthPower>(2),
        new DynamicVar("TargetStrength", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ThoughtMechanics.CreateHoverTip(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public NymphFaceOff()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        int narrated = await ThoughtMechanics.Narrate(
            choiceContext,
            cardPlay,
            DynamicVars["Narrate"].IntValue);
        if (narrated <= 0)
        {
            return;
        }

        await CreatureCmd.GainBlock(
            Owner.Creature,
            narrated,
            ValueProp.Unpowered,
            cardPlay);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Strength.IntValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["TargetStrength"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1);
    }
}
