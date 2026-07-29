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
public sealed class NymphRoughWaterRecovery : ModCardTemplate
{
    protected override bool ShouldGlowGoldInternal =>
        ThoughtMechanics.GetState(Owner) == ThoughtState.Confused;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath:
            $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath:
            $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16, ValueProp.Move),
        new DynamicVar("Refund", 1)
    ];

    public NymphRoughWaterRecovery()
        : base(
            2,
            CardType.Attack,
            CardRarity.Common,
            TargetType.AnyEnemy,
            true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool wasConfused =
            ThoughtMechanics.GetState(Owner) == ThoughtState.Confused;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (wasConfused)
        {
            await PlayerCmd.GainEnergy(
                DynamicVars["Refund"].IntValue,
                Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Refund"].UpgradeValueBy(1);
    }
}
