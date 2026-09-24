using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Cards;
using Nymph.Mechanics;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Monsters;

[RegisterMonster]
public sealed class NymphTheresis : ModMonsterTemplate
{
    internal const string SlashMoveId = "SOVEREIGN_SLASH";
    internal const string StrengthenMoveId = "SOVEREIGN_STRENGTHEN";
    private const string VisualsScenePath =
        $"{Entry.ResPath}/scenes/monsters/Theresis.tscn";
    private MoveState? _phaseTwoIdle;
    private bool _showingSlashIntent;

    public override int MinInitialHp => 200;
    public override int MaxInitialHp => 200;
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public override IEnumerable<string> AssetPaths => [VisualsScenePath];

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        STS2RitsuLib.Scaffolding.Godot.RitsuGodotNodeFactories
            .CreateFromScenePath<NCreatureVisuals>(VisualsScenePath);

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("C1_Idle", isLooping: true);
        AnimState attack = new("C1_Attack");
        AnimState cast = new("C1_Skill_2");
        AnimState slashBegin = new("C1_Skill_1_Begin");
        AnimState slashLoop = new("C1_Skill_1_Loop", isLooping: true);
        AnimState slashEnd = new("C1_Skill_1_End");
        AnimState move = new("C1_Move", isLooping: true);
        AnimState hit = new("C1_Idle");
        AnimState dead = new("C2_Skill_Die");
        AnimState phaseTwoIdle = new("C2_Idle", isLooping: true);
        AnimState reviveBegin = new("C1_Revive_Begin");
        AnimState reviveLoop = new("C1_Revive_Loop", isLooping: true);
        AnimState reviveEnd = new("C1_Revive_End");
        AnimState phaseTwoSkillBegin = new("C2_Skill_Begin");
        AnimState phaseTwoSkillLoop = new(
            "C2_Skill_Loop",
            isLooping: true);
        attack.NextState = idle;
        cast.NextState = idle;
        slashBegin.NextState = slashLoop;
        slashEnd.NextState = idle;
        hit.NextState = idle;
        reviveBegin.NextState = reviveLoop;
        reviveEnd.NextState = phaseTwoIdle;
        phaseTwoSkillBegin.NextState = phaseTwoSkillLoop;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("SlashBegin", slashBegin);
        animator.AddAnyState("SlashEnd", slashEnd);
        animator.AddAnyState("Move", move);
        animator.AddAnyState(
            "Hit",
            hit,
            () => !_showingSlashIntent
                && Creature.GetPower<TheresisTwinPower>()?.SecondPhase
                    != true);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("ReviveBegin", reviveBegin);
        animator.AddAnyState("ReviveEnd", reviveEnd);
        animator.AddAnyState("PhaseTwoSkill", phaseTwoSkillBegin);
        animator.AddAnyState("PhaseTwoIdle", phaseTwoIdle);
        return animator;
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<TheresisTwinPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            1,
            Creature,
            null,
            silent: true);
        await PowerCmd.Apply<TheresisSovereignAfterimagePower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            1,
            Creature,
            null,
            silent: true);
        await SetSlashPresentation(true);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState slash = new(
            SlashMoveId,
            Slash,
            new SingleAttackIntent(18));
        MoveState guard = new(
            "SOVEREIGN_GUARD",
            AttackAndGiveGuard,
            new SingleAttackIntent(25),
            new StatusIntent(1));
        MoveState strengthen = new(
            StrengthenMoveId,
            Strengthen,
            new BuffIntent());
        _phaseTwoIdle = new MoveState(
            "SOVEREIGN_REVIVING",
            _ => Task.CompletedTask);

        slash.FollowUpState = guard;
        guard.FollowUpState = strengthen;
        strengthen.FollowUpState = slash;
        _phaseTwoIdle.FollowUpState = _phaseTwoIdle;
        return new MonsterMoveStateMachine(
            [slash, guard, strengthen, _phaseTwoIdle],
            slash);
    }

    internal void EnterSecondPhase()
    {
        if (_phaseTwoIdle is not null)
        {
            SetMoveImmediate(_phaseTwoIdle, forceTransition: true);
        }
    }

    private async Task Slash(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(18)
            .FromMonster(this)
            .WithAttackerAnim("SlashEnd", 0.45f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        _showingSlashIntent = false;
    }

    private async Task AttackAndGiveGuard(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(25)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.55f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        foreach (var player in CombatState.Players
            .Where(player => !player.Creature.IsDead))
        {
            NymphGuard guard = CombatState.CreateCard<NymphGuard>(player);
            await CardPileCmd.AddGeneratedCardToCombat(
                guard,
                MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand,
                player);
        }

    }

    private async Task Strengthen(IReadOnlyList<Creature> targets)
    {
        List<Creature> allies = CombatState.Enemies
            .Where(enemy => !enemy.IsDead
                && (enemy == Creature
                    || enemy.Monster is NymphSovereignShadow))
            .ToList();
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            allies,
            5,
            Creature,
            null);
    }

    internal async Task SetSlashPresentation(bool shouldAttack)
    {
        if (_showingSlashIntent == shouldAttack)
        {
            return;
        }

        _showingSlashIntent = shouldAttack;
        await CreatureCmd.TriggerAnim(
            Creature,
            shouldAttack ? "SlashBegin" : "Idle",
            0f);
    }

    internal async Task RestoreSlashPresentationAfterMove()
    {
        if (NextMove.Id != SlashMoveId)
        {
            return;
        }

        _showingSlashIntent = true;
        await CreatureCmd.TriggerAnim(Creature, "SlashBegin", 0f);
    }

    internal async Task SyncShadowIntents(bool shouldAttack)
    {
        foreach (NymphSovereignShadow shadow in CombatState.Enemies
            .Where(enemy => !enemy.IsDead)
            .Select(enemy => enemy.Monster)
            .OfType<NymphSovereignShadow>())
        {
            await shadow.SetSlashIntent(shouldAttack);
        }
    }
}

[RegisterMonster]
public sealed class NymphTheresa : ModMonsterTemplate
{
    private const string VisualsScenePath =
        $"{Entry.ResPath}/scenes/monsters/Theresa.tscn";
    private MoveState? _waitOne;
    private MoveState? _waitTwo;
    private MoveState? _willShock;
    private MoveState? _negatedShock;

    public override int MinInitialHp => 200;
    public override int MaxInitialHp => 200;
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public override IEnumerable<string> AssetPaths => [VisualsScenePath];

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        STS2RitsuLib.Scaffolding.Godot.RitsuGodotNodeFactories
            .CreateFromScenePath<NCreatureVisuals>(VisualsScenePath);

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("C1_Idle", isLooping: true);
        AnimState cast = new("C1_Skill_1");
        AnimState hit = new("C1_Idle");
        AnimState dead = new("C2_Revive_Begin");
        AnimState phaseTwoIdle = new("C2_Idle", isLooping: true);
        AnimState reviveStart = new("C2_Revive_Begin");
        AnimState reviveLoop = new("C2_Revive_Loop", isLooping: true);
        AnimState reviveEnd = new("C2_Revive_End");
        AnimState phaseTwoSkillBegin = new("C2_Skill_Begin");
        AnimState phaseTwoSkillLoop = new(
            "C2_Skill_Loop",
            isLooping: true);
        AnimState phaseTwoSkillEnd = new("C2_Skill_End");
        cast.NextState = idle;
        hit.NextState = idle;
        reviveStart.NextState = reviveLoop;
        reviveEnd.NextState = phaseTwoIdle;
        phaseTwoSkillBegin.NextState = phaseTwoSkillLoop;
        phaseTwoSkillEnd.NextState = phaseTwoSkillLoop;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Attack", cast);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("ReviveStart", reviveStart);
        animator.AddAnyState("ReviveEnd", reviveEnd);
        animator.AddAnyState(
            "PhaseTwoSkillBeginLoop",
            phaseTwoSkillBegin);
        animator.AddAnyState("PhaseTwoSkillEnd", phaseTwoSkillEnd);
        animator.AddAnyState("PhaseTwoIdle", phaseTwoIdle);
        return animator;
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        ThrowingPlayerChoiceContext context = new();
        await PowerCmd.Apply<TheresaWillShockPower>(
            context, Creature, 1, Creature, null, silent: true);
        SetUntargetableVisuals();
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _waitOne = new MoveState("WILL_SHOCK_WAIT_ONE", Wait);
        _waitTwo = new MoveState("WILL_SHOCK_WAIT_TWO", Wait);
        _willShock = new MoveState(
            "WILL_SHOCK",
            Shock,
            new SingleAttackIntent(10),
            new DebuffIntent());
        _negatedShock = new MoveState(
            "WILL_SHOCK_NEGATED",
            _ => Task.CompletedTask);

        _waitOne.FollowUpState = _waitTwo;
        _waitTwo.FollowUpState = _willShock;
        _willShock.FollowUpState = _waitOne;
        _negatedShock.FollowUpState = _waitOne;
        return new MonsterMoveStateMachine(
            [_waitOne, _waitTwo, _willShock, _negatedShock],
            _waitOne);
    }

    internal void NegateCurrentIntent()
    {
        TheresaWillShockPower? power =
            Creature.GetPower<TheresaWillShockPower>();
        if (_willShock is null
            || _negatedShock is null
            || NextMove.Id != _willShock.Id)
        {
            return;
        }

        bool secondPhase = power?.SecondPhase == true;
        _negatedShock.FollowUpState = secondPhase ? _willShock : _waitOne;
        SetMoveImmediate(_negatedShock, forceTransition: true);
        power?.SetTurnsRemaining(secondPhase ? 1 : 3);
    }

    internal void EnterSecondPhase()
    {
        if (_willShock is not null)
        {
            _willShock.FollowUpState = _willShock;
            SetMoveImmediate(_willShock, forceTransition: true);
        }

        Creature.GetPower<TheresaWillShockPower>()?.EnterSecondPhase();
    }

    internal void RefreshShockIntent(int damage)
    {
        if (_willShock is not null)
        {
            _willShock.Intents =
            [
                new SingleAttackIntent(damage),
                new DebuffIntent()
            ];
        }
    }

    private Task Wait(IReadOnlyList<Creature> targets)
    {
        Creature.GetPower<TheresaWillShockPower>()?.AdvanceCountdown();
        return Task.CompletedTask;
    }

    private async Task Shock(IReadOnlyList<Creature> targets)
    {
        TheresaWillShockPower? power =
            Creature.GetPower<TheresaWillShockPower>();
        int damage = power?.ShockDamage ?? 10;
        await DamageCmd.Attack(damage)
            .FromMonster(this)
            .WithAttackerAnim(
                power?.SecondPhase == true ? "PhaseTwoSkillEnd" : "Cast",
                0.55f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        foreach (var player in CombatState.Players.Where(
            player => !player.Creature.IsDead))
        {
            bool cursed = CurseRandomCards(player, PileType.Draw);
            cursed |= CurseRandomCards(player, PileType.Discard);
            if (cursed)
            {
                await PowerCmd.Apply<CursePollutionThisTurnPower>(
                    new ThrowingPlayerChoiceContext(),
                    player.Creature,
                    1,
                    Creature,
                    null,
                    silent: true);
            }
        }

        power?.AfterShock();
    }

    private static bool CurseRandomCards(
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        PileType pileType)
    {
        List<CardModel> candidates = pileType.GetPile(player).Cards
            .Where(card => !card.Keywords.Contains(
                NymphKeywords.CursePollution))
            .ToList();
        player.RunState.Rng.Shuffle.Shuffle(candidates);
        List<CardModel> selected = candidates.Take(2).ToList();
        foreach (CardModel card in selected)
        {
            CardCmd.ApplyKeyword(card, NymphKeywords.CursePollution);
        }

        return selected.Count > 0;
    }

    private void SetUntargetableVisuals()
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(Creature);
        CanvasGroup? canvas =
            node?.GetSpecialNode<CanvasGroup>("%CanvasGroup");
        canvas?.SetSelfModulate(StsColors.halfTransparentWhite);

        if (node?._stateDisplay is { } stateDisplay)
        {
            stateDisplay._healthBar.Visible = false;
            stateDisplay._hpBarHitbox.MouseFilter =
                Control.MouseFilterEnum.Ignore;
        }
    }
}

[RegisterMonster]
public sealed class NymphSovereignShadow : ModMonsterTemplate
{
    private const string VisualsScenePath =
        $"{Entry.ResPath}/scenes/monsters/SovereignShadow.tscn";
    private int _slotIndex;
    private MoveState? _slashIntent;
    private MoveState? _idleIntent;
    private bool _showingSlashIntent;
    private bool _startsWithSlash;

    public override int MinInitialHp => 100;
    public override int MaxInitialHp => 100;
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public int SlotIndex
    {
        get => _slotIndex;
        set
        {
            AssertMutable();
            _slotIndex = value;
        }
    }

    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public bool StartsWithSlash
    {
        get => _startsWithSlash;
        set
        {
            AssertMutable();
            _startsWithSlash = value;
        }
    }

    public override IEnumerable<string> AssetPaths => [VisualsScenePath];

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        STS2RitsuLib.Scaffolding.Godot.RitsuGodotNodeFactories
            .CreateFromScenePath<NCreatureVisuals>(VisualsScenePath);

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("Idle", isLooping: true);
        AnimState spawn = new("Start");
        AnimState slashBegin = new("Skill_1_Begin");
        AnimState slashLoop = new("Skill_1_Loop", isLooping: true);
        AnimState slashEnd = new("Skill_1_End");
        AnimState hit = new("Idle");
        AnimState dead = new("Die");
        spawn.NextState = idle;
        slashBegin.NextState = slashLoop;
        slashEnd.NextState = idle;
        hit.NextState = idle;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Spawn", spawn);
        animator.AddAnyState("SlashBegin", slashBegin);
        animator.AddAnyState("SlashEnd", slashEnd);
        animator.AddAnyState("Hit", hit, () => !_showingSlashIntent);
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
        await CreatureCmd.TriggerAnim(Creature, "Spawn", 0.6f);

        NymphTheresis? theresis = CombatState.Enemies
            .Where(enemy => !enemy.IsDead)
            .Select(enemy => enemy.Monster)
            .OfType<NymphTheresis>()
            .FirstOrDefault();
        bool shouldAttack = StartsWithSlash
            || theresis?.NextMove.Id == NymphTheresis.SlashMoveId;
        StartsWithSlash = false;
        await SetSlashIntent(shouldAttack);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _slashIntent = new MoveState(
            "SHADOW_SLASH",
            Slash,
            new SingleAttackIntent(18));
        _idleIntent = new MoveState(
            "SHADOW_IDLE",
            _ => Task.CompletedTask);
        _slashIntent.FollowUpState = _slashIntent;
        _idleIntent.FollowUpState = _idleIntent;
        return new MonsterMoveStateMachine(
            [_slashIntent, _idleIntent],
            _slashIntent);
    }

    internal async Task SetSlashIntent(bool shouldAttack)
    {
        MoveState? state = shouldAttack ? _slashIntent : _idleIntent;
        if (state is not null)
        {
            SetMoveImmediate(state, forceTransition: true);
        }

        if (_showingSlashIntent == shouldAttack)
        {
            return;
        }

        _showingSlashIntent = shouldAttack;
        await CreatureCmd.TriggerAnim(
            Creature,
            shouldAttack ? "SlashBegin" : "Idle",
            0f);
    }

    private async Task Slash(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(18)
            .FromMonster(this)
            .WithAttackerAnim("SlashEnd", 0.45f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        _showingSlashIntent = false;
    }
}
