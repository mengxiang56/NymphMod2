using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Encounters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace Nymph.Monsters;

[RegisterMonster]
public sealed class SanctifierAutomaton : ModMonsterTemplate
{
    private const string VisualsScenePath =
        $"{Entry.ResPath}/scenes/monsters/SanctifierAutomaton.tscn";
    private int _distance;

    public override int MinInitialHp => 30;
    public override int MaxInitialHp => 30;
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;
    public override IEnumerable<string> AssetPaths => [VisualsScenePath];

    [SavedProperty]
    public int Distance
    {
        get => _distance;
        set
        {
            AssertMutable();
            _distance = value;
        }
    }

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            VisualsScenePath);

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("Idle", isLooping: true);
        AnimState hit = new("Idle");
        AnimState dead = new("Die");
        hit.NextState = idle;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Dead", dead);
        return animator;
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<MinionPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            1,
            Creature,
            null,
            silent: true);
        await PowerCmd.Apply<SanctifierAutomatonSelfDestructPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            1,
            Creature,
            null);
        BodrakastiBoss.AlignAutomatonSlots(CombatState);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idle = new("AUTOMATON_IDLE", _ => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine([idle], idle);
    }
}
