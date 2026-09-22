using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Encounters;
using Nymph.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class TheresisSovereignAfterimagePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
    protected override bool IsVisibleInternal =>
        Owner?.GetPower<TheresisTwinPower>()?.SecondPhase != true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/HeartToHeartPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/HeartToHeartPower84.png");
}

[RegisterPower]
public sealed class TheresisTwinPower : ModPowerTemplate
{
    private const float ShadowDeathTimeScale = 2f;
    private readonly Dictionary<Creature, NCreature> _dyingShadowNodes = [];
    private readonly Dictionary<Creature, Task> _shadowDeathWaits = [];
    private int _distance;
    private bool _secondPhase;
    private int _phaseTwoDecay;
    private bool _transitioning;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
    protected override bool IsVisibleInternal => !SecondPhase;
    internal bool IsTransitioning => _transitioning;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/HeartToHeartPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/HeartToHeartPower84.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Reduction", 75)
    ];

    [SavedProperty]
    public int Distance
    {
        get => _distance;
        set
        {
            AssertMutable();
            _distance = Math.Clamp(value, 0, 3);
            SyncReductionVar();
        }
    }

    [SavedProperty]
    public bool SecondPhase
    {
        get => _secondPhase;
        set
        {
            AssertMutable();
            _secondPhase = value;
            SyncReductionVar();
        }
    }

    [SavedProperty]
    public int PhaseTwoDecay
    {
        get => _phaseTwoDecay;
        set
        {
            AssertMutable();
            _phaseTwoDecay = Math.Clamp(value, 0, 3);
            SyncReductionVar();
        }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
#if !STS2_PUBLIC
        ,
        CardPlay? cardPlay
#endif
        )
    {
        if (target != Owner)
        {
            return 1m;
        }

        int steps = SecondPhase ? PhaseTwoDecay : Distance;
        return Math.Clamp(0.25m + steps * 0.25m, 0.25m, 1m);
    }

    public override bool ShouldDie(Creature creature) =>
        creature != Owner || SecondPhase;

    public override bool ShouldStopCombatFromEnding() =>
        _transitioning;

    public override Task BeforeDeath(Creature creature)
    {
        if (creature.Monster is NymphSovereignShadow
            && NCombatRoom.Instance?.GetCreatureNode(creature) is { } node)
        {
            _dyingShadowNodes[creature] = node;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner || SecondPhase || _transitioning)
        {
            return;
        }

        _transitioning = true;
        SecondPhase = true;
        PhaseTwoDecay = 0;
        try
        {
            Creature? theresa = Owner.CombatState!.Enemies.FirstOrDefault(
                enemy => enemy.Monster is NymphTheresa && !enemy.IsDead);

            await KillShadowsAndWaitForAnimations();

            Task lordReviveStart = PlayAnimationToCompletion(
                Owner,
                "ReviveBegin",
                1f);
            Task sageReviveStart = theresa is null
                ? Task.CompletedTask
                : PlayAnimationToCompletion(
                    theresa,
                    "ReviveStart",
                    1f);
            await Task.WhenAll(lordReviveStart, sageReviveStart);

            await CreatureCmd.Heal(Owner, Owner.MaxHp, playAnim: false);

            if (Owner.Monster is NymphTheresis theresis)
            {
                theresis.EnterSecondPhase();
            }

            if (theresa?.Monster is NymphTheresa theresaModel)
            {
                theresaModel.EnterSecondPhase();
            }

            await PowerCmd.Apply<TheresisTwinbornPower>(
                new ThrowingPlayerChoiceContext(),
                Owner,
                75,
                Owner,
                null,
                silent: true);

            Task lordReviveEnd = PlayAnimationToCompletion(
                Owner,
                "ReviveEnd",
                0.8f);
            Task sageReviveEnd = theresa is null
                ? Task.CompletedTask
                : PlayAnimationToCompletion(
                    theresa,
                    "ReviveEnd",
                    0.8f);
            await Task.WhenAll(lordReviveEnd, sageReviveEnd);

            TeleportCreatureToSlot(
                Owner,
                TheresisTheresaBoss.ShadowSlot2);
            MirrorCreatureVisuals(Owner);
            if (theresa is not null)
            {
                await FadeTeleportAndFadeIn(
                    theresa,
                    TheresisTheresaBoss.PhaseTwoSageSlot);
            }

            await CreatureCmd.TriggerAnim(Owner, "PhaseTwoSkill", 0f);
            if (theresa is not null)
            {
                await CreatureCmd.TriggerAnim(
                    theresa,
                    "PhaseTwoSkillBeginLoop",
                    0f);
            }
        }
        finally
        {
            _transitioning = false;
        }
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        _dyingShadowNodes.Remove(creature, out NCreature? deathNode);
        Task deathWait = creature.Monster is NymphSovereignShadow
            ? StartAcceleratedDeathAnimationWait(
                deathNode,
                deathAnimLength)
            : Task.CompletedTask;
        if (creature.Monster is NymphSovereignShadow)
        {
            _shadowDeathWaits[creature] = deathWait;
        }

        if (SecondPhase
            || Owner.IsDead
            || creature.Monster is not NymphSovereignShadow shadow
            || shadow.SlotIndex != 3 - Distance
            || Distance >= 3)
        {
            return;
        }

        await deathWait;
        _shadowDeathWaits.Remove(creature);

        await MoveAwayAndCreateShadows();
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy)
        {
            return;
        }

        if (SecondPhase && PhaseTwoDecay < 3)
        {
            PhaseTwoDecay++;
        }
        else if (!SecondPhase && Owner.Monster is NymphTheresis theresis)
        {
            bool shouldAttack =
                theresis.NextMove.Id == NymphTheresis.StrengthenMoveId;
            await theresis.SetSlashPresentation(shouldAttack);
            await theresis.SyncShadowIntents(shouldAttack);
        }
    }

    private async Task MoveAwayAndCreateShadows()
    {
        int oldLordIndex = 4 - Distance;
        Distance++;
        int newLordIndex = oldLordIndex - 1;
        await MoveCreatureToSlot(
            Owner,
            TheresisTheresaBoss.GetShadowSlot(newLordIndex));

        await AddShadow(oldLordIndex);
        if (Distance < 3)
        {
            await AddShadow(newLordIndex - 1);
        }
    }

    private async Task AddShadow(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 4 || Owner.CombatState is null)
        {
            return;
        }

        NymphSovereignShadow shadow =
            (NymphSovereignShadow)ModelDb.Monster<NymphSovereignShadow>()
                .ToMutable();
        shadow.SlotIndex = slotIndex;
        await CreatureCmd.Add(
            shadow,
            Owner.CombatState,
            CombatSide.Enemy,
            TheresisTheresaBoss.GetShadowSlot(slotIndex));
    }

    private async Task KillShadowsAndWaitForAnimations()
    {
        if (Owner.CombatState is null)
        {
            return;
        }

        List<Creature> shadows = Owner.CombatState.Enemies
            .Where(enemy => enemy.Monster is NymphSovereignShadow
                && !enemy.IsDead)
            .ToList();
        await CreatureCmd.Kill(shadows, force: true);

        Task[] deathTasks = shadows
            .Select(shadow => _shadowDeathWaits.Remove(
                    shadow,
                    out Task? deathWait)
                ? deathWait
                : Task.CompletedTask)
            .ToArray();
        if (deathTasks.Length > 0)
        {
            await Task.WhenAll(deathTasks);
        }
    }

    private static Task StartAcceleratedDeathAnimationWait(
        NCreature? node,
        float fallbackDuration)
    {
        float unscaledDuration = fallbackDuration;

        if (node is not null)
        {
            using MegaTrackEntry? track =
                node.SpineAnimation.GetCurrentTrack();
            if (track is not null)
            {
                float trackRemaining = Math.Max(
                    0f,
                    track.GetAnimationEnd() - track.GetTrackTime());
                if (trackRemaining > 0f)
                {
                    unscaledDuration = trackRemaining;
                }

                track.SetTimeScale(ShadowDeathTimeScale);
            }
        }

        if (unscaledDuration <= 0f
            && node?.DeathAnimationTask is { } deathTask)
        {
            return deathTask;
        }

        float acceleratedDuration =
            Math.Max(0f, unscaledDuration / ShadowDeathTimeScale);
        return Cmd.Wait(acceleratedDuration, ignoreCombatEnd: true);
    }

    private void SyncReductionVar()
    {
        int steps = SecondPhase ? PhaseTwoDecay : Distance;
        int reduction = Math.Max(0, 75 - steps * 25);
        DynamicVars["Reduction"].BaseValue = reduction;
        if (Owner?.GetPower<TheresisTwinbornPower>() is { } twinborn)
        {
            twinborn.Reduction = reduction;
        }
    }

    private static async Task MoveCreatureToSlot(
        Creature creature,
        string slotName)
    {
        creature.SlotName = slotName;
        if (NCombatRoom.Instance?.EncounterSlots is not { } slots
            || NCombatRoom.Instance.GetCreatureNode(creature) is not { } node)
        {
            return;
        }

        Marker2D marker = slots.GetNode<Marker2D>(slotName);
        await CreatureCmd.TriggerAnim(creature, "Move", 0f);
        Tween tween = node.CreateTween();
        tween.TweenProperty(
                node,
                "global_position",
                marker.GlobalPosition,
                2.0f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        await tween.AwaitFinished(node);
        await CreatureCmd.TriggerAnim(creature, "Idle", 0f);
        if (creature.Monster is NymphTheresis theresis)
        {
            await theresis.RestoreSlashPresentationAfterMove();
        }
    }

    private static async Task PlayAnimationToCompletion(
        Creature creature,
        string trigger,
        float fallbackDuration)
    {
        await CreatureCmd.TriggerAnim(creature, trigger, 0f);
        await Cmd.CustomScaledWait(0.03f, 0.05f);
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(creature);
        float duration = node?.GetCurrentAnimationTimeRemaining()
            ?? fallbackDuration;
        if (duration <= 0f)
        {
            duration = fallbackDuration;
        }

        await Cmd.CustomScaledWait(
            Mathf.Min(duration * 0.5f, 0.25f),
            duration);
    }

    private static void TeleportCreatureToSlot(
        Creature creature,
        string slotName)
    {
        creature.SlotName = slotName;
        if (NCombatRoom.Instance?.EncounterSlots is not { } slots
            || NCombatRoom.Instance.GetCreatureNode(creature) is not { } node)
        {
            return;
        }

        node.GlobalPosition = slots.GetNode<Marker2D>(slotName).GlobalPosition;
    }

    private static async Task FadeTeleportAndFadeIn(
        Creature creature,
        string slotName)
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(creature);
        CanvasGroup? canvas =
            node?.GetSpecialNode<CanvasGroup>("%CanvasGroup");
        if (node is null || canvas is null)
        {
            TeleportCreatureToSlot(creature, slotName);
            return;
        }

        Tween fadeOut = canvas.CreateTween();
        fadeOut.TweenProperty(canvas, "self_modulate:a", 0f, 0.65f);
        await fadeOut.AwaitFinished(canvas);

        TeleportCreatureToSlot(creature, slotName);

        Tween fadeIn = canvas.CreateTween();
        fadeIn.TweenProperty(
            canvas,
            "self_modulate:a",
            StsColors.halfTransparentWhite.A,
            0.65f);
        await fadeIn.AwaitFinished(canvas);
    }

    private static void MirrorCreatureVisuals(Creature creature)
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(creature);
        Node2D? visuals = node?.GetSpecialNode<Node2D>("%Visuals");
        if (visuals is null)
        {
            return;
        }

        Vector2 scale = visuals.Scale;
        scale.X = Mathf.Abs(scale.X);
        visuals.Scale = scale;
    }
}

[RegisterPower]
public sealed class TheresisTwinbornPower : ModPowerTemplate
{
    private int _reduction = 75;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/HeartToHeartPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/HeartToHeartPower84.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Reduction", 75)
    ];

    [SavedProperty]
    public int Reduction
    {
        get => _reduction;
        set
        {
            AssertMutable();
            _reduction = Math.Clamp(value, 0, 75);
            DynamicVars["Reduction"].BaseValue = _reduction;
        }
    }
}

[RegisterPower]
public sealed class TheresaWillShockPower : ModPowerTemplate
{
    private int _shockDamage = 10;
    private int _turnsRemaining = 3;
    private bool _secondPhase;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
    public override bool OwnerIsSecondaryEnemy => true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/DreadkazEchoPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/DreadkazEchoPower84.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Damage", 10),
        new DynamicVar("TurnsRemaining", 3)
    ];

    [SavedProperty]
    public int ShockDamage
    {
        get => _shockDamage;
        set
        {
            AssertMutable();
            _shockDamage = Math.Max(0, value);
            DynamicVars["Damage"].BaseValue = _shockDamage;
            if (Owner?.Monster is NymphTheresa theresa)
            {
                theresa.RefreshShockIntent(_shockDamage);
            }
        }
    }

    [SavedProperty]
    public int TurnsRemaining
    {
        get => _turnsRemaining;
        set
        {
            AssertMutable();
            _turnsRemaining = Math.Max(1, value);
            DynamicVars["TurnsRemaining"].BaseValue = _turnsRemaining;
        }
    }

    [SavedProperty]
    public bool SecondPhase
    {
        get => _secondPhase;
        set
        {
            AssertMutable();
            _secondPhase = value;
        }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
#if !STS2_PUBLIC
        ,
        CardPlay? cardPlay
#endif
        ) => target == Owner ? 0m : 1m;

    public override bool ShouldAllowHitting(Creature creature) =>
        creature != Owner;

    public override bool ShouldAllowTargeting(Creature target) =>
        target != Owner;

    public override bool ShouldDie(Creature creature) =>
        creature != Owner;

    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
    public override bool ShouldOwnerDeathTriggerFatal() => false;

    public override bool ShouldStopCombatFromEnding()
    {
        TheresisTwinPower? twin = Owner.CombatState?.Enemies
            .Where(enemy => enemy.Monster is NymphTheresis)
            .Select(enemy => enemy.GetPower<TheresisTwinPower>())
            .FirstOrDefault(power => power is not null);
        return twin is not null
            && (!twin.SecondPhase || twin.IsTransitioning);
    }

    internal void SetTurnsRemaining(int turns) => TurnsRemaining = turns;

    internal void AdvanceCountdown()
    {
        TurnsRemaining = Math.Max(1, TurnsRemaining - 1);
    }

    internal void AfterShock()
    {
        ShockDamage += 10;
        TurnsRemaining = SecondPhase ? 1 : 3;
    }

    internal void EnterSecondPhase()
    {
        SecondPhase = true;
        TurnsRemaining = 1;
    }

}
