using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace Nymph.Monsters;

public abstract class QuilonMonsterBase : ModMonsterTemplate
{
    protected abstract string SceneName { get; }
    protected abstract string IdleAnimation { get; }
    protected virtual string AttackAnimation => "Attack";
    protected virtual string CastAnimation => "Skill";
    protected virtual string DeathAnimation => "Die";

    public override MonsterAssetProfile AssetProfile => new($"{Entry.ResPath}/scenes/monsters/{SceneName}.tscn");
    public override bool HasDeathSfx => false;
    public override string? HurtSfx => null;

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        AnimState idle = new(IdleAnimation, true);
        AnimState attack = new(AttackAnimation) { NextState = idle };
        AnimState cast = new(CastAnimation) { NextState = idle };
        AnimState hit = new(IdleAnimation) { NextState = idle };
        AnimState dead = new(DeathAnimation);
        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Dead", dead);
        return animator;
    }
}

[RegisterMonster]
public sealed class Quilon : QuilonMonsterBase
{
    private int _buffStep;
    private bool _awakened;
    protected override string SceneName => "quilon";
    protected override string IdleAnimation => "B_Idle";
    protected override string AttackAnimation => "C_Attack";
    protected override string CastAnimation => "B_Skill_2";
    protected override string DeathAnimation => "C_Die";
    public override int MinInitialHp => 400;
    public override int MaxInitialHp => 400;
    public override bool IsHealthBarVisible => _awakened;
    private int TwinDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 12);
    private int FiveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    public override bool ShouldAllowHitting(Creature creature) =>
        creature != Creature || _awakened;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = [];
        MoveState awaken = new("AWAKEN_MOVE", Awaken, new SummonIntent(), new BuffIntent());
        MoveState twin = new("TWIN_STRIKE_MOVE", TwinStrike, new MultiAttackIntent(TwinDamage, 2));
        MoveState five = new("FIVE_PRECEPTS_MOVE", FivePrecepts, new MultiAttackIntent(FiveDamage, 5));
        MoveState rally = new("RALLY_MOVE", Rally, new SummonIntent(), new BuffIntent());
        RandomBranchState random = new("RANDOM");
        random.AddBranch(twin, 1, MoveRepeatType.CanRepeatForever);
        random.AddBranch(five, 1, MoveRepeatType.CanRepeatForever);
        random.AddBranch(rally, 2, MoveRepeatType.CanRepeatForever);
        awaken.FollowUpState = random;
        twin.FollowUpState = random;
        five.FollowUpState = random;
        rally.FollowUpState = random;
        states.AddRange([awaken, twin, five, rally, random]);
        return new MonsterMoveStateMachine(states, awaken);
    }

    private async Task Awaken(IReadOnlyList<Creature> targets)
    {
        _awakened = true;
        NCombatRoom.Instance?.SetCreatureIsInteractable(Creature, on: true);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.45f);
        await PowerCmd.Apply<TrikayaPower>(new ThrowingPlayerChoiceContext(), Creature, 100, Creature, null);
        await PowerCmd.Apply<BreathPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<NilaFireWardPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<ExhaustFirePower>(new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null);
        await SummonMissing();
    }

    private Task TwinStrike(IReadOnlyList<Creature> targets) =>
        DamageCmd.Attack(TwinDamage).WithHitCount(2).FromMonster(this).OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.45f).WithHitFx("vfx/vfx_attack_slash").Execute(null);

    private Task FivePrecepts(IReadOnlyList<Creature> targets) =>
        DamageCmd.Attack(FiveDamage).WithHitCount(5).FromMonster(this).OnlyPlayAnimOnce()
            .WithAttackerAnim("Cast", 0.45f).WithHitFx("vfx/vfx_attack_slash").Execute(null);

    private async Task Rally(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.35f);
        await SummonMissing();
        int strength = _buffStep switch { 0 => 0, 1 => 4, 2 => 0, 3 => 6, _ => 10 };
        if (_buffStep == 0)
            await PowerCmd.Apply<ResistPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        else if (_buffStep == 2)
            await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null);
        else
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, strength, Creature, null);
        _buffStep++;
    }

    private async Task SummonMissing()
    {
        if (!CombatState.Enemies.Any(c => c.IsAlive && c.Monster is Envies))
            await CreatureCmd.Add<Envies>(CombatState, "envies");
        if (!CombatState.Enemies.Any(c => c.IsAlive && c.Monster is Furies))
            await CreatureCmd.Add<Furies>(CombatState, "furies");
        if (!CombatState.Enemies.Any(c => c.IsAlive && c.Monster is ChaliceOfRegret))
            await CreatureCmd.Add<ChaliceOfRegret>(CombatState, "chalice");
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != Creature || wasRemovalPrevented) return;
        List<Creature> summons = CombatState.Enemies
            .Where(c => c.IsAlive && c != Creature && c.Monster is Envies or Furies or ChaliceOfRegret)
            .ToList();
        if (summons.Count > 0)
            await CreatureCmd.Kill(summons, force: true);
    }
}

[RegisterMonster]
public sealed class Envies : QuilonMonsterBase
{
    protected override string SceneName => "envies";
    protected override string IdleAnimation => "Idle";
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 30, 25);
    public override int MaxInitialHp => MinInitialHp;
    private int Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState curse = new("ENVY_CURSE_MOVE", Curse, new DebuffIntent());
        MoveState attack = new("ENVY_ATTACK_MOVE", Attack, new SingleAttackIntent(Damage));
        curse.FollowUpState = attack; attack.FollowUpState = curse;
        return new MonsterMoveStateMachine([curse, attack], curse);
    }
    private Task Curse(IReadOnlyList<Creature> targets) => PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, 2, Creature, null);
    private Task Attack(IReadOnlyList<Creature> targets) => DamageCmd.Attack(Damage).FromMonster(this).WithAttackerAnim("Attack", .45f).WithHitFx("vfx/vfx_attack_slash").Execute(null);
}

[RegisterMonster]
public sealed class Furies : QuilonMonsterBase
{
    protected override string SceneName => "furies";
    protected override string IdleAnimation => "Idle";
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 45);
    public override int MaxInitialHp => MinInitialHp;
    private int Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 12);
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState attack = new("FURY_ATTACK_MOVE", Attack, new SingleAttackIntent(Damage));
        attack.FollowUpState = attack;
        return new MonsterMoveStateMachine([attack], attack);
    }
    private Task Attack(IReadOnlyList<Creature> targets) => DamageCmd.Attack(Damage).FromMonster(this).WithAttackerAnim("Attack", .45f).WithHitFx("vfx/vfx_attack_slash").Execute(null);
}

[RegisterMonster]
public sealed class ChaliceOfRegret : QuilonMonsterBase
{
    protected override string SceneName => "chalice_of_regret";
    protected override string IdleAnimation => "Idle";
    protected override string AttackAnimation => "Idle";
    protected override string CastAnimation => "Idle";
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 80, 70);
    public override int MaxInitialHp => MinInitialHp;
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState wait = new("REGRET_WAIT_MOVE", _ => Task.CompletedTask, new DefendIntent());
        wait.FollowUpState = wait;
        return new MonsterMoveStateMachine([wait], wait);
    }
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<RegretPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        foreach (Creature ally in CombatState.Enemies.Where(c => c != Creature && c.IsAlive))
            await PowerCmd.Apply<TransDamagePower>(new ThrowingPlayerChoiceContext(), ally, 1, Creature, null);
    }
}
