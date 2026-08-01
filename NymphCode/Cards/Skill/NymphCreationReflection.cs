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
public sealed class NymphCreationReflection : ModCardTemplate
{
    private const int BaseEnergyGain = 2;
    private const int UpgradedEnergyGain = 3;

    private int EnergyGain =>
        CurrentUpgradeLevel > 0 ? UpgradedEnergyGain : BaseEnergyGain;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Cards", 2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<NymphDreadkaz>()
    ];

    public NymphCreationReflection()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(EnergyGain, Owner);
        await PowerCmd.Apply<DreadkazEchoPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Cards"].IntValue,
            Owner.Creature,
            this);
    }
}
