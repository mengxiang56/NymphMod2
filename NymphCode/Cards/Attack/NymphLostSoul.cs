using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphLostSoul : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move),
        new PowerVar<NecrosisPower>(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>()
    ];

    public NymphLostSoul()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        if (!cardPlay.Target.IsDead)
        {
            await PowerCmd.Apply<NecrosisPower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars["NecrosisPower"].IntValue,
                Owner.Creature,
                this);
            await PowerCmd.Apply<NecrosisPermanentLockPower>(
                choiceContext,
                cardPlay.Target,
                1,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
        DynamicVars["NecrosisPower"].UpgradeValueBy(1);
    }
}
