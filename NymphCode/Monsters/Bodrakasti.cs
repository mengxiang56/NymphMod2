using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Nymph.Cards;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace Nymph.Monsters;

[RegisterMonster]
public sealed class Bodrakasti : ModMonsterTemplate
{
    private const string VisualsScenePath =
        $"{Entry.ResPath}/scenes/monsters/Bodrakasti.tscn";
    private MoveState? _phaseTwoMultiAttack;
    private MoveState? _reviveState;

    public override int MinInitialHp => 300;
    public override int MaxInitialHp => 300;
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public override IEnumerable<string> AssetPaths => [VisualsScenePath];

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            VisualsScenePath);

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("C1_Idle", isLooping: true);
        AnimState attack = new("C1_Attack");
        AnimState cast = new("C1_Skill_1");
        AnimState hit = new("C1_Idle");
        AnimState phaseOneDie = new("C1_Die");
        AnimState phaseOneDead = new("C1_Die");
        AnimState revive = new("C1_Die_End");
        AnimState phaseTwoIdle = new("C2_Idle", isLooping: true);
        AnimState phaseTwoMulti = new("C2_Attack");
        AnimState phaseTwoHeavy = new("C2_Attack_2");
        AnimState phaseTwoSkill = new("C2_Skill_1");
        AnimState phaseTwoHit = new("C2_Idle");
        AnimState dead = new("C2_Die");
        attack.NextState = idle;
        cast.NextState = idle;
        hit.NextState = idle;
        revive.NextState = phaseTwoIdle;
        phaseTwoMulti.NextState = phaseTwoIdle;
        phaseTwoHeavy.NextState = phaseTwoIdle;
        phaseTwoSkill.NextState = phaseTwoIdle;
        phaseTwoHit.NextState = phaseTwoIdle;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState(
            "Hit",
            hit,
            () => Creature.GetPower<BodrakastiPhasePower>()
                ?.SecondPhase != true);
        animator.AddAnyState(
            "Hit",
            phaseTwoHit,
            () => Creature.GetPower<BodrakastiPhasePower>()
                ?.SecondPhase == true);
        animator.AddAnyState("PhaseOneDie", phaseOneDie);
        animator.AddAnyState("Revive", revive);
        animator.AddAnyState("PhaseTwoIdle", phaseTwoIdle);
        animator.AddAnyState("PhaseTwoMulti", phaseTwoMulti);
        animator.AddAnyState("PhaseTwoHeavy", phaseTwoHeavy);
        animator.AddAnyState("PhaseTwoSkill", phaseTwoSkill);
        animator.AddAnyState(
            "Dead",
            phaseOneDead,
            () => Creature.GetPower<BodrakastiPhasePower>()
                ?.SecondPhase != true);
        animator.AddAnyState(
            "Dead",
            dead,
            () => Creature.GetPower<BodrakastiPhasePower>()
                ?.SecondPhase == true);
        return animator;
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        ThrowingPlayerChoiceContext context = new();
        await ApplyRulePower<BodrakastiHolyCarePower>(context);
        await ApplyRulePower<BodrakastiCounselPower>(context);
        await ApplyRulePower<BodrakastiGuidancePower>(context);
        await ApplyRulePower<BodrakastiPhasePower>(context);

        foreach (var player in CombatState.Players)
        {
            if (!player.Creature.IsDead
                && player.Creature.GetPower<BodrakastiDistancePower>() is null)
            {
                await PowerCmd.Apply<BodrakastiDistancePower>(
                    context,
                    player.Creature,
                    2,
                    Creature,
                    null,
                    silent: true);
            }
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState ceremony = new(
            "CEREMONY",
            Ceremony,
            new StatusIntent(1),
            new BuffIntent());
        MoveState attack = new(
            "ATTACK",
            Attack,
            new SingleAttackIntent(40));
        _phaseTwoMultiAttack = new MoveState(
            "PHASE_TWO_MULTI_ATTACK",
            PhaseTwoMultiAttack,
            new MultiAttackIntent(10, 5));
        _reviveState = new MoveState(
            "RESPAWN_MOVE",
            ReviveMove,
            new HealIntent(),
            new BuffIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };
        MoveState phaseTwoHeavyAttack = new(
            "PHASE_TWO_HEAVY_ATTACK",
            PhaseTwoHeavyAttack,
            new SingleAttackIntent(40));
        MoveState phaseTwoReinforce = new(
            "PHASE_TWO_REINFORCE",
            PhaseTwoReinforce,
            new BuffIntent(),
            new StatusIntent(2));
        ceremony.FollowUpState = attack;
        attack.FollowUpState = attack;
        _phaseTwoMultiAttack.FollowUpState = phaseTwoHeavyAttack;
        phaseTwoHeavyAttack.FollowUpState = phaseTwoReinforce;
        phaseTwoReinforce.FollowUpState = _phaseTwoMultiAttack;
        _reviveState.FollowUpState = _phaseTwoMultiAttack;
        return new MonsterMoveStateMachine(
        [
            ceremony,
            attack,
            _reviveState,
            _phaseTwoMultiAttack,
            phaseTwoHeavyAttack,
            phaseTwoReinforce
        ], ceremony);
    }

    internal void EnterReviveState()
    {
        if (_reviveState is not null)
        {
            SetMoveImmediate(_reviveState, forceTransition: true);
        }
    }

    private async Task ReviveMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Revive", 0f);
        await Cmd.Wait(0.8f, ignoreCombatEnd: true);
        if (Creature.GetPower<BodrakastiPhasePower>() is { } phase)
        {
            await phase.ReviveForSecondPhase();
        }
    }

    private async Task Ceremony(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        ThrowingPlayerChoiceContext context = new();
        await PowerCmd.Apply<BodrakastiHolyCityEmbracePower>(
            context,
            Creature,
            5,
            Creature,
            null);
        await PowerCmd.Apply<RitualPower>(
            context,
            Creature,
            10,
            Creature,
            null);

        foreach (var player in CombatState.Players.Where(
            player => !player.Creature.IsDead))
        {
            if (PileType.Hand.GetPile(player).Cards.Count
                >= CardPile.MaxCardsInHand)
            {
                continue;
            }

            NymphBodrakastiRitual ritual =
                CombatState.CreateCard<NymphBodrakastiRitual>(player);
            await CardPileCmd.AddGeneratedCardToCombat(
                ritual,
                PileType.Hand,
                player);
        }
    }

    private Task Attack(IReadOnlyList<Creature> targets) =>
        DamageCmd.Attack(40)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.6f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

    private Task PhaseTwoMultiAttack(IReadOnlyList<Creature> targets) =>
        DamageCmd.Attack(10)
            .WithHitCount(5)
            .FromMonster(this)
            .WithAttackerAnim("PhaseTwoMulti", 0.6f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

    private Task PhaseTwoHeavyAttack(IReadOnlyList<Creature> targets) =>
        DamageCmd.Attack(40)
            .FromMonster(this)
            .WithAttackerAnim("PhaseTwoHeavy", 0.6f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

    private async Task PhaseTwoReinforce(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "PhaseTwoSkill", 0.5f);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            5,
            Creature,
            null);

        foreach (var player in CombatState.Players.Where(
            player => !player.Creature.IsDead))
        {
            for (int i = 0; i < 2; i++)
            {
                PileType destination =
                    PileType.Hand.GetPile(player).Cards.Count
                        < CardPile.MaxCardsInHand
                        ? PileType.Hand
                        : PileType.Discard;
                NymphSanctifierAutomaton automaton =
                    CombatState.CreateCard<NymphSanctifierAutomaton>(player);
                await CardPileCmd.AddGeneratedCardToCombat(
                    automaton,
                    destination,
                    player);
            }
        }
    }

    private Task ApplyRulePower<T>(ThrowingPlayerChoiceContext context)
        where T : PowerModel =>
        PowerCmd.Apply<T>(
            context,
            Creature,
            1,
            Creature,
            null,
            silent: true);
}
