using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphLeadBreastplate : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.Conceive
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(14, ValueProp.Move),
        new DynamicVar("Create", 6),
        new DynamicVar("FreeNarratePower", 1)
    ];

    public NymphLeadBreastplate()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await ThoughtMechanics.Create(
            choiceContext,
            Owner,
            DynamicVars["Create"].IntValue,
            this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        await PowerCmd.Apply<FreeNarratePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["FreeNarratePower"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}
