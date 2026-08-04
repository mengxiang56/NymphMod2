using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Nymph.Characters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphKeyToHeart : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_power_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StrengthLoss", 2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>(),
        HoverTipFactory.FromPower<KeyToHeartStrengthDownPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public NymphKeyToHeart()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<KeyToHeartPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["StrengthLoss"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthLoss"].UpgradeValueBy(1);
    }
}
