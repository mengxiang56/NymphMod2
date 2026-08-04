using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Nymph.Characters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphEmpathy : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<NecrosisPower>(3),
        new DynamicVar("Interval", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>()
    ];

    public NymphEmpathy()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        IEnumerable<Creature> enemies = CombatState!
            .GetOpponentsOf(Owner.Creature)
            .Where(opponent => !opponent.IsDead);

        await PowerCmd.Apply<NecrosisPower>(
            choiceContext,
            enemies,
            DynamicVars["NecrosisPower"].IntValue,
            Owner.Creature,
            this);

        await PowerCmd.Apply<NecrosisDecayIntervalPower>(
            choiceContext,
            enemies.Where(enemy => !enemy.IsDead),
            DynamicVars["Interval"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Interval"].UpgradeValueBy(1);
    }
}
