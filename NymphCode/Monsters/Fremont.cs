using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace Nymph.Monsters;

[RegisterMonster]
public sealed class Fremont : ModMonsterTemplate
{
    private const string VisualsScenePath =
        $"{Entry.ResPath}/scenes/monsters/Fremont.tscn";
    private const string AttackOneMove = "ATTACK_ONE_MOVE";
    private const string AttackTwoMove = "ATTACK_TWO_MOVE";
    private const string DebuffMove = "DEBUFF_MOVE";
    private const string CleanseMove = "CLEANSE_MOVE";
    private const string PhaseTwoAttackOneMove = "PHASE_TWO_ATTACK_ONE_MOVE";
    private const string PhaseTwoAttackTwoMove = "PHASE_TWO_ATTACK_TWO_MOVE";

    private MoveState? _cleanseState;
    private MoveState? _phaseTwoAttackOneState;

    protected override string AttackSfx =>
        "event:/sfx/enemy/enemy_attacks/queen/queen_arms_attack";

    public override int MinInitialHp => 500;
    public override int MaxInitialHp => 500;
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public override IEnumerable<string> AssetPaths =>
        new[] { VisualsScenePath }
            .Concat(ModelDb.Orb<LightningOrb>().AssetPaths);

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            VisualsScenePath);

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("Idle", isLooping: true);
        AnimState attack = new("Attack");
        AnimState cast = new("Charge");
        AnimState hit = new("Idle");
        AnimState dead = new("C2_Revive_Die");

        attack.NextState = idle;
        cast.NextState = idle;
        hit.NextState = idle;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Dead", dead);
        return animator;
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<FremontMechanicsPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            1,
            Creature,
            null,
            silent: true);
        await ApplyRulePower<FremontBlockEvokeRulePower>();
        await ApplyRulePower<FremontCardChannelRulePower>();
        await ApplyRulePower<FremontBlackCoffinRulePower>();
        await ApplyRulePower<FremontSecondPhaseRulePower>();
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState attackOne = new(
            AttackOneMove,
            targets => Attack(targets, 25),
            new SingleAttackIntent(25));
        MoveState attackTwo = new(
            AttackTwoMove,
            targets => AttackAndBlock(targets),
            new MultiAttackIntent(6, 3),
            new DefendIntent());
        MoveState debuff = new(
            DebuffMove,
            BuffAndDebuff,
            new BuffIntent(),
            new DebuffIntent());

        _cleanseState = new MoveState(
            CleanseMove,
            Cleanse,
            new BuffIntent());
        _phaseTwoAttackOneState = new MoveState(
            PhaseTwoAttackOneMove,
            targets => Attack(targets, 25, applyNecrosis: true),
            new SingleAttackIntent(25),
            new DebuffIntent());
        MoveState phaseTwoAttackTwo = new(
            PhaseTwoAttackTwoMove,
            targets => AttackAndBlock(targets, applyNecrosis: true),
            new MultiAttackIntent(6, 3),
            new DefendIntent(),
            new DebuffIntent());

        attackOne.FollowUpState = attackTwo;
        attackTwo.FollowUpState = debuff;
        debuff.FollowUpState = attackOne;

        _cleanseState.FollowUpState = _phaseTwoAttackOneState;
        _phaseTwoAttackOneState.FollowUpState = phaseTwoAttackTwo;
        phaseTwoAttackTwo.FollowUpState = debuff;

        return new MonsterMoveStateMachine(
        [
            attackOne,
            attackTwo,
            debuff,
            _cleanseState,
            _phaseTwoAttackOneState,
            phaseTwoAttackTwo
        ], attackOne);
    }

    internal void EnterSecondPhase()
    {
        if (_cleanseState is not null)
        {
            SetMoveImmediate(_cleanseState);
        }
    }

    private async Task Attack(
        IReadOnlyList<Creature> targets,
        int damage,
        bool applyNecrosis = false)
    {
        await DamageCmd.Attack(damage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.6f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        if (applyNecrosis)
        {
            await ApplyNecrosis(targets);
        }
    }

    private async Task AttackAndBlock(
        IReadOnlyList<Creature> targets,
        bool applyNecrosis = false)
    {
        await DamageCmd.Attack(6)
            .WithHitCount(3)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.6f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CreatureCmd.GainBlock(Creature, 25, ValueProp.Move, null);

        if (applyNecrosis)
        {
            await ApplyNecrosis(targets);
        }
    }

    private async Task BuffAndDebuff(IReadOnlyList<Creature> targets)
    {
        ThrowingPlayerChoiceContext choiceContext = new();
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Creature,
            3,
            Creature,
            null);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            targets,
            1,
            Creature,
            null);
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            targets,
            1,
            Creature,
            null);
    }

    private async Task Cleanse(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        foreach (PowerModel debuff in Creature.Powers
            .Where(power => power.TypeForCurrentAmount == PowerType.Debuff)
            .ToList())
        {
            await PowerCmd.Remove(debuff);
        }
    }

    private Task ApplyNecrosis(IReadOnlyList<Creature> targets) =>
        PowerCmd.Apply<NecrosisPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            2,
            Creature,
            null);

    private Task ApplyRulePower<T>() where T : PowerModel =>
        PowerCmd.Apply<T>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            1,
            Creature,
            null,
            silent: true);
}
