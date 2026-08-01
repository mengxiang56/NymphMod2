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
    private const int BaseEnergyGain = 1;
    private const int UpgradedEnergyGain = 2;

    private int EnergyGain =>
        CurrentUpgradeLevel > 0 ? UpgradedEnergyGain : BaseEnergyGain;

    protected override bool ShouldGlowGoldInternal =>
        ThoughtMechanics.GetState(Owner) != ThoughtState.Clear
        && ThoughtMechanics.CanNarrate(
            Owner,
            DynamicVars["Narrate"].IntValue);

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.Narrate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath:
            $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath:
            $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16, ValueProp.Move),
        new DynamicVar("Narrate", 6)
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
        bool notClear =
            ThoughtMechanics.GetState(Owner, cardPlay) != ThoughtState.Clear;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (!notClear)
        {
            return;
        }

        int narrated = await ThoughtMechanics.Narrate(
            choiceContext,
            cardPlay,
            DynamicVars["Narrate"].IntValue);
        if (narrated <= 0)
        {
            return;
        }

        await PlayerCmd.GainEnergy(
            EnergyGain
                * ThoughtMechanics.NarrationEffectMultiplier(Owner),
            Owner);
    }
}
