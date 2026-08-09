using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphExtensiveKnowledge : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_power_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Threshold", 4),
        new PowerVar<ExtensiveKnowledgePower>(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        NymphKeywords.CreateThresholdHoverTip(),
        HoverTipFactory.FromPower<LucidPower>()
    ];

    public NymphExtensiveKnowledge()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await ThoughtMechanics.IncreaseConfusedThreshold(
            choiceContext,
            Owner,
            DynamicVars["Threshold"].IntValue,
            this);
        await PowerCmd.Apply<ExtensiveKnowledgePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["ExtensiveKnowledgePower"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Threshold"].UpgradeValueBy(2);
    }
}
