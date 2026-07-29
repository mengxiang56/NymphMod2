using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphNewBranch : ModCardTemplate
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
        new DamageVar(6, ValueProp.Move),
        new DynamicVar("Create", 3),
        new CardsVar(1)
    ];

    public NymphNewBranch()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy, true)
    {
    }

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (card != this || CombatState is null)
        {
            return;
        }

        var target = Owner.RunState.Rng.CombatTargets.NextItem(
            CombatState
                .GetOpponentsOf(Owner.Creature)
                .Where(enemy => !enemy.IsDead));
        if (target is not null)
        {
            await CardCmd.AutoPlay(choiceContext, this, target);
        }
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

        if (!Owner.Creature.HasPower<NewBranchDrawUsedPower>())
        {
            await PowerCmd.Apply<NewBranchDrawUsedPower>(
                choiceContext,
                Owner.Creature,
                1,
                Owner.Creature,
                this,
                silent: true);
            await CardPileCmd.Draw(
                choiceContext,
                DynamicVars.Cards.IntValue,
                Owner);
        }

        var copy = CombatState!.CreateCard<NymphNewBranch>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(
                copy,
                MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
        }

        await CardPileCmd.AddGeneratedCardsToCombat(
            new CardModel[] { copy },
            PileType.Discard,
            Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}
