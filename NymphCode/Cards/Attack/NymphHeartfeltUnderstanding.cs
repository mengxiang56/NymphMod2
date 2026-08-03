using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphHeartfeltUnderstanding : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move)
    ];

    public NymphHeartfeltUnderstanding()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (!cardPlay.Target.IsDead
            && cardPlay.Target.Monster is { } monster
            && monster.MoveStateMachine is { } moveStateMachine)
        {
            MoveState skippedMove = monster.NextMove;
            bool mustPerformOnce =
                skippedMove.MustPerformOnceBeforeTransitioning;
            skippedMove.MustPerformOnceBeforeTransitioning = false;
            try
            {
                moveStateMachine.OnMovePerformed(skippedMove);
                monster.RollMove(CombatState!.PlayerCreatures);
            }
            finally
            {
                skippedMove.MustPerformOnceBeforeTransitioning =
                    mustPerformOnce;
            }

            cardPlay.Target.PrepareForNextTurn(
                CombatState!.PlayerCreatures,
                rollNewMove: false);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
