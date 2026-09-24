using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Monsters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class NymphGuard : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Ethereal,
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/NymphGuard.png");

    public NymphGuard()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.Self, false)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            20,
            DamageProps.cardUnpowered,
            Owner.Creature,
            this,
            cardPlay);
        Creature? theresa = Owner.Creature.CombatState?.Enemies
            .FirstOrDefault(enemy => enemy.Monster is NymphTheresa);
        if (theresa?.Monster is NymphTheresa model)
        {
            model.NegateCurrentIntent();
        }
    }

    protected override void OnUpgrade()
    {
    }
}
