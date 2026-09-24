using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Nymph.Encounters;
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
        AnimState phaseTwoMultiBegin = new("C2_Skill_2_Begin");
        AnimState phaseTwoMultiLoop = new(
            "C2_Skill_2_Loop",
            isLooping: true);
        AnimState phaseTwoMultiEnd = new("C2_Skill_2_End");
        AnimState phaseTwoHeavy = new("C2_Attack_2");
        AnimState phaseTwoSkill = new("C2_Skill_1");
        AnimState phaseTwoReviveSkillBegin = new("C2_Skill_3_Begin");
        AnimState phaseTwoReviveSkillLoop = new(
            "C2_Skill_3_Loop",
            isLooping: true);
        AnimState phaseTwoReviveSkillEnd = new("C2_Skill_3_End");
        AnimState phaseTwoHit = new("C2_Idle");
        AnimState dead = new("C2_Die");
        attack.NextState = idle;
        cast.NextState = idle;
        hit.NextState = idle;
        revive.NextState = phaseTwoIdle;
        phaseTwoMultiBegin.NextState = phaseTwoMultiLoop;
        phaseTwoMultiEnd.NextState = phaseTwoIdle;
        phaseTwoHeavy.NextState = phaseTwoIdle;
        phaseTwoSkill.NextState = phaseTwoIdle;
        phaseTwoReviveSkillBegin.NextState = phaseTwoReviveSkillLoop;
        phaseTwoReviveSkillEnd.NextState = phaseTwoIdle;
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
        animator.AddAnyState("PhaseTwoMultiBegin", phaseTwoMultiBegin);
        animator.AddAnyState("PhaseTwoMultiEnd", phaseTwoMultiEnd);
        animator.AddAnyState("PhaseTwoHeavy", phaseTwoHeavy);
        animator.AddAnyState("PhaseTwoSkill", phaseTwoSkill);
        animator.AddAnyState(
            "PhaseTwoReviveSkillBegin",
            phaseTwoReviveSkillBegin);
        animator.AddAnyState(
            "PhaseTwoReviveSkillEnd",
            phaseTwoReviveSkillEnd);
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
        await ApplyRulePower<BodrakastiPhasePower>(context);
        await PowerCmd.Apply<BodrakastiHolyCityEmbracePower>(
            context,
            Creature,
            5,
            Creature,
            null);
        await PowerCmd.Apply<StrengthPower>(
            context,
            Creature,
            10,
            Creature,
            null);

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

        BodrakastiBoss.AlignAutomatonSlots(CombatState);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState ceremony = new(
            "CEREMONY",
            Ceremony,
            new BuffIntent(),
            new SummonIntent());
        MoveState firstAttack = new(
            "FIRST_ATTACK",
            Attack,
            new SingleAttackIntent(40));
        MoveState secondAttack = new(
            "SECOND_ATTACK",
            Attack,
            new SingleAttackIntent(40));
        MoveState summon = new(
            "SUMMON_AUTOMATONS",
            SummonMove,
            new SummonIntent());
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
            new SummonIntent());
        ceremony.FollowUpState = firstAttack;
        firstAttack.FollowUpState = secondAttack;
        secondAttack.FollowUpState = summon;
        summon.FollowUpState = firstAttack;
        _phaseTwoMultiAttack.FollowUpState = phaseTwoHeavyAttack;
        phaseTwoHeavyAttack.FollowUpState = phaseTwoReinforce;
        phaseTwoReinforce.FollowUpState = _phaseTwoMultiAttack;
        _reviveState.FollowUpState = _phaseTwoMultiAttack;
        return new MonsterMoveStateMachine(
        [
            ceremony,
            firstAttack,
            secondAttack,
            summon,
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
            await CreatureCmd.TriggerAnim(
                Creature,
                "PhaseTwoReviveSkillBegin",
                0.8f);
            await CreatureCmd.TriggerAnim(
                Creature,
                "PhaseTwoReviveSkillEnd",
                0f);
        }
    }

    private async Task Ceremony(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        ThrowingPlayerChoiceContext context = new();
        await ApplyRulePower<BodrakastiGuidancePower>(context);
        await ApplyRulePower<BodrakastiCounselPower>(context);
        await PowerCmd.Apply<RitualPower>(
            context,
            Creature,
            10,
            Creature,
            null);
        await SummonAutomatons();
    }

    private async Task SummonMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        await SummonAutomatons();
    }

    private async Task SummonAutomatons()
    {
        BodrakastiBoss.AlignAutomatonSlots(CombatState);
        HashSet<int> playerDistances = CombatState.Players
            .Where(player => !player.Creature.IsDead)
            .Select(player => player.Creature
                .GetPower<BodrakastiDistancePower>()?.Amount ?? 2)
            .ToHashSet();

        for (int distance = 1; distance <= 3; distance++)
        {
            if (playerDistances.Contains(distance)
                || CombatState.Enemies.Any(enemy => !enemy.IsDead
                    && enemy.Monster is SanctifierAutomaton automaton
                    && automaton.Distance == distance))
            {
                continue;
            }

            SanctifierAutomaton automaton =
                (SanctifierAutomaton)ModelDb.Monster<SanctifierAutomaton>()
                    .ToMutable();
            automaton.Distance = distance;
            await CreatureCmd.Add(
                automaton,
                CombatState,
                CombatSide.Enemy,
                BodrakastiBoss.GetAutomatonSlot(distance));
        }
    }

    private Task Attack(IReadOnlyList<Creature> targets) =>
        DamageCmd.Attack(40)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.6f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

    private async Task PhaseTwoMultiAttack(
        IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(10)
            .WithHitCount(5)
            .FromMonster(this)
            .WithAttackerAnim("PhaseTwoMultiBegin", 0.6f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CreatureCmd.TriggerAnim(
            Creature,
            "PhaseTwoMultiEnd",
            0f);
    }

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
        await SummonAutomatons();
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
