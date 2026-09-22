using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(StatusCardPool))]
public sealed class NymphSanctifierAutomaton : ModCardTemplate
{
    private static readonly ThrowingPlayerChoiceContext ChoiceContext = new();

    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Unpowered | ValueProp.Move)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/NymphDreadkaz.png");

    public NymphSanctifierAutomaton()
        : base(1, CardType.Status, CardRarity.Status, TargetType.Self, false)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) => Task.CompletedTask;

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (card != this
            || oldPileType == PileType.Discard
            || card.Pile?.Type != PileType.Discard
            || Owner.Creature.IsDead)
        {
            return;
        }

        await CreatureCmd.Damage(
            ChoiceContext,
            Owner.Creature,
            DynamicVars.Damage,
            this,
            null);
    }

    protected override void OnUpgrade()
    {
    }
}
