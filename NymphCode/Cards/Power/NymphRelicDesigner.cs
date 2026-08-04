using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphRelicDesigner : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.RecreateRelic
    ];

    public override CardAssetProfile AssetProfile =>
        ContentAssetProfiles.AncientCard(
            $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
            GetType().Name,
            CardType.Power);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.RecreateRelicId)
    ];

    public NymphRelicDesigner()
        : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(
            Owner.Creature,
            "PowerUp",
            Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<RelicDesignerPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
