using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Nymph.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(StatusCardPool))]
public sealed class NymphNilaFire : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/NymphNilaFire.png");
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("SelfDamage", 10, ValueProp.Unpowered | ValueProp.Unblockable),
        new DynamicVar("StrengthLoss", 2)
    ];

    public NymphNilaFire() : base(0, CardType.Status, CardRarity.Status, TargetType.Self, false) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.Damage, this, cardPlay);
        await PowerCmd.Apply<NilaFireStrengthDownPower>(choiceContext, Owner.Creature, DynamicVars["StrengthLoss"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() { }
}
