using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Cards;
using Nymph.Encounters;
using Nymph.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class BodrakastiDistancePower : ModPowerTemplate
{
    internal const float DistanceStep = 240f;
    internal const float PlayerForwardOffset = 120f;
    private float _middlePositionX;
    private bool _openingRitualGranted;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    protected override bool IsVisibleInternal => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/HeartToHeartPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/HeartToHeartPower84.png");

    [SavedProperty]
    public bool OpeningRitualGranted
    {
        get => _openingRitualGranted;
        set
        {
            AssertMutable();
            _openingRitualGranted = value;
        }
    }

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner.Player || OpeningRitualGranted)
        {
            return;
        }

        OpeningRitualGranted = true;
        if (Owner.IsDead
            || PileType.Hand.GetPile(player).Cards.Count
                >= CardPile.MaxCardsInHand)
        {
            return;
        }

        NymphBodrakastiRitual ritual =
            CombatState.CreateCard<NymphBodrakastiRitual>(player);
        await CardPileCmd.AddGeneratedCardToCombat(
            ritual,
            PileType.Hand,
            player);
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (node is not null)
        {
            _middlePositionX = node.GlobalPosition.X + PlayerForwardOffset;
        }

        return UpdateCreaturePositions();
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power == this)
        {
            await UpdateCreaturePositions();
            BodrakastiBoss.UpdateAutomatonFacings(CombatState);
        }
    }

    internal async Task ChangeDistance(
        PlayerChoiceContext choiceContext,
        int delta,
        CardModel cardSource)
    {
        int next = Math.Clamp(Amount + delta, 1, 3);
        int actualDelta = next - Amount;
        if (actualDelta == 0)
        {
            Flash();
            return;
        }

        await PowerCmd.Apply<BodrakastiDistancePower>(
            choiceContext,
            Owner,
            actualDelta,
            Owner,
            cardSource,
            silent: true);
    }

    private async Task UpdateCreaturePositions()
    {
        if (NCombatRoom.Instance is not { } room
            || room.GetCreatureNode(Owner) is not { } ownerNode)
        {
            return;
        }

        if (_middlePositionX == 0f)
        {
            _middlePositionX = ownerNode.GlobalPosition.X + PlayerForwardOffset;
        }

        float ownerTargetX = _middlePositionX
            + (Amount - 2) * DistanceStep;
        float offset = ownerTargetX - ownerNode.GlobalPosition.X;
        if (Math.Abs(offset) <= 1f)
        {
            return;
        }

        List<Creature> affected = [Owner];
        affected.AddRange(Owner.Pets);

        Tween tween = room.CreateTween()
            .SetParallel()
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        foreach (Creature creature in affected.Where(c => !c.IsDead))
        {
            if (room.GetCreatureNode(creature) is not { } node)
            {
                continue;
            }

            tween.TweenProperty(
                node,
                "global_position:x",
                node.GlobalPosition.X + offset,
                0.25f);
        }

        await tween.AwaitFinished(room);
    }

    internal static decimal GetMultiplier(Creature? creature)
    {
        int distance = creature?.GetPower<BodrakastiDistancePower>()
            ?.Amount ?? 2;
        return distance switch
        {
            1 => 0.5m,
            3 => 2m,
            _ => 1m
        };
    }
}

[RegisterPower]
public sealed class BodrakastiHolyCarePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/BodrakastiHolyCarePower64.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/BodrakastiHolyCarePower84.png");

    public override decimal ModifyDamageAdditive(
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
        if (dealer != Owner || target?.Player is null)
        {
            return 0m;
        }

        decimal strengthBeforeGuidance = 0m;
        if (props.IsPoweredAttack())
        {
            strengthBeforeGuidance =
                (Owner.GetPower<StrengthPower>()?.Amount ?? 0)
                + (Owner.GetPower<BodrakastiGuidanceStrengthLossPower>()
                    ?.Amount ?? 0);
        }

        decimal middleDamage = amount + Math.Max(0m, strengthBeforeGuidance);
        return middleDamage
            * (BodrakastiDistancePower.GetMultiplier(target) - 1m);
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
        if (target == Owner && dealer?.Player is not null)
        {
            return BodrakastiDistancePower.GetMultiplier(dealer) * 0.5m;
        }

        return 1m;
    }
}

[RegisterPower]
public sealed class BodrakastiCounselPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/BodrakastiCounselPower64.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/BodrakastiCounselPower84.png");

    public override Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        CardModel card = cardPlay.Card;
        if (Owner.IsDead
            || Owner.GetPower<BodrakastiPhasePower>()?.SecondPhase != true
            || card.Owner.Creature.Side == Owner.Side
            || (card.Pool != card.Owner.Character.CardPool
                && !ModelDb.AllCharacterCardPools.Contains(card.Pool)))
        {
            return Task.CompletedTask;
        }

        Flash();
        card.EnergyCost.AddThisCombat(1);
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class BodrakastiHolyCityEmbracePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/HolyCityEmbracePower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/HolyCityEmbracePower84.png");

    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource) =>
        target == Owner ? Math.Max(0m, amount - Amount) : amount;
}

[RegisterPower]
public sealed class BodrakastiGuidancePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/BodrakastiGuidancePower64.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/BodrakastiGuidancePower84.png");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner
            || result.UnblockedDamage <= 0
            || Owner.GetPower<BodrakastiPhasePower>()?.SecondPhase == true)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<BodrakastiGuidanceStrengthLossPower>(
            choiceContext,
            Owner,
            result.UnblockedDamage,
            Owner,
            cardSource,
            silent: true);
    }
}

[RegisterPower]
public sealed class BodrakastiPhasePower : ModPowerTemplate
{
    private bool _secondPhase;
    private bool _waitingToRevive;
    private int _preservedStrength;
    private int _preservedEmbrace;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
    protected override bool IsVisibleInternal => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/HolyCityEmbracePower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/HolyCityEmbracePower84.png");

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

    [SavedProperty]
    public bool WaitingToRevive
    {
        get => _waitingToRevive;
        set
        {
            AssertMutable();
            _waitingToRevive = value;
        }
    }

    // Legacy save data only; phase two no longer inherits Strength.
    [SavedProperty]
    public int PreservedStrength
    {
        get => _preservedStrength;
        set
        {
            AssertMutable();
            _preservedStrength = value;
        }
    }

    // Keep the old saved field so runs started before this change can load.
    [SavedProperty]
    public int PreservedEmbrace
    {
        get => _preservedEmbrace;
        set
        {
            AssertMutable();
            _preservedEmbrace = value;
        }
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (creature != Owner
            || SecondPhase
            || WaitingToRevive
            || wasRemovalPrevented)
        {
            return;
        }

        WaitingToRevive = true;
        if (Owner.Monster is Bodrakasti bodrakasti)
        {
            bodrakasti.EnterReviveState();
        }

        await Task.CompletedTask;
    }

    public override bool ShouldAllowHitting(Creature creature) =>
        creature != Owner || !WaitingToRevive;

    public override bool ShouldAllowTargeting(Creature target) =>
        target != Owner || !WaitingToRevive;

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(
        Creature creature) => creature != Owner || SecondPhase;

    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    public override bool ShouldStopCombatFromEnding() => !SecondPhase;

    internal async Task ReviveForSecondPhase()
    {
        if (SecondPhase || !WaitingToRevive)
        {
            return;
        }

        ThrowingPlayerChoiceContext context = new();
        WaitingToRevive = false;
        await CreatureCmd.SetMaxHp(Owner, Owner.MaxHp);
        await CreatureCmd.Heal(Owner, Owner.MaxHp);
        if (Owner.GetPower<ArtifactPower>() is { } oldArtifact)
        {
            await PowerCmd.Remove(oldArtifact);
        }

        ArtifactPower? artifact = await PowerCmd.Apply<ArtifactPower>(
            context,
            Owner,
            2,
            Owner,
            null);
        artifact?.SetAmount(2, silent: true);
        if (Owner.GetPower<BodrakastiCounselPower>() is { } counsel)
        {
            await PowerCmd.Remove(counsel);
        }
        await PowerCmd.Apply<BodrakastiCounselPower>(
            context, Owner, 1, Owner, null, silent: true);
        await PowerCmd.Apply<BodrakastiHolyCarePower>(
            context, Owner, 1, Owner, null, silent: true);
        PreservedStrength = 0;
        if (Owner.GetPower<StrengthPower>() is { } strength)
        {
            await PowerCmd.Remove(strength);
        }

        await GiveHolyCityShields();
        SecondPhase = true;
    }

    internal async Task GiveHolyCityShields()
    {
        if (Owner.CombatState is not { } combatState)
        {
            return;
        }

        foreach (var player in combatState.Players.Where(
            player => !player.Creature.IsDead))
        {
            NymphHolyCityShield shield =
                combatState.CreateCard<NymphHolyCityShield>(player);
            await CardPileCmd.AddGeneratedCardToCombat(
                shield,
                PileType.Hand,
                player);
        }
    }
}

[RegisterPower]
public sealed class BodrakastiGuidanceStrengthLossPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/BodrakastiGuidancePower64.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/BodrakastiGuidancePower84.png");

    public override async Task BeforeApplied(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            target,
            -amount,
            applier,
            cardSource,
            silent: true);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount == Amount)
        {
            return;
        }

        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            -amount,
            applier,
            cardSource,
            silent: true);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        decimal strengthToRestore = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            strengthToRestore,
            Owner,
            null,
            silent: true);
    }
}
