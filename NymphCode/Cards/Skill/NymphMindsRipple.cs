using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Nymph.Characters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphMindsRipple : ModCardTemplate
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<NecrosisPower>(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>()
    ];

    public NymphMindsRipple()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int x = ResolveEnergyXValue();
        int applyTimes = x + (IsUpgraded ? 1 : 0);
        for (int i = 0; i < applyTimes; i++)
        {
            await PowerCmd.Apply<NecrosisPower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars["NecrosisPower"].IntValue,
                Owner.Creature,
                this);
        }

        if (cardPlay.Target.GetPower<NecrosisPower>() is { } necrosis)
        {
            await necrosis.Trigger(
                choiceContext,
                Owner.Creature,
                this,
                x);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
