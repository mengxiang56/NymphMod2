using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(StatusCardPool))]
public sealed class NymphDreadkaz : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override bool HasTurnEndInHandEffect => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable,
        CardKeyword.Ethereal
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath:
            $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Necrosis", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>()
    ];

    public NymphDreadkaz()
        : base(
            -1,
            CardType.Status,
            CardRarity.Status,
            TargetType.None,
            false)
    {
    }

    protected override async Task OnTurnEndInHand(
        PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<NecrosisPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Necrosis"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
    }
}
