using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class NecrosisPower :
    ModPowerTemplate,
    IPowerExtraIconAmountLabelSpecsProvider,
    IPowerExtraIconAmountLabelsChangeSource
{
    private sealed class CardPlayRecord
    {
        public required int AmountAtPlayStart { get; init; }

        public required bool AdvancesReduction { get; init; }
    }

    private sealed class CardPlayData
    {
        public Dictionary<CardModel, CardPlayRecord> AmountsAtPlayStart { get; } = [];
    }

    internal const int DefaultCardsPerLayerLoss = 3;
    private int _cardsPlayedTowardLayerLoss;
    private int _cardsPerLayerLoss = DefaultCardsPerLayerLoss;
    private int _triggersTowardKeyEffect;
    private bool _reductionLockedPermanently;
    private bool _reductionLockedThisTurn;

    public event Action? PowerExtraIconAmountLabelsInvalidated;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType =>
        PowerInstanceType.InstancedPerApplier;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardsRemaining", DefaultCardsPerLayerLoss)
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/NecrosisPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/NecrosisPower84.png");

    public override LocString Description =>
        new LocString(
            "powers",
            IsCanonical
                ? "NYMPH_POWER_NECROSIS_POWER.description"
                : Owner?.IsPlayer == true
                    ? "NYMPH_POWER_NECROSIS_POWER.descriptionOnPlayer"
                    : "NYMPH_POWER_NECROSIS_POWER.descriptionOnEnemy");

    protected override string SmartDescriptionLocKey =>
        !IsCanonical && Owner?.IsPlayer == true
            ? "NYMPH_POWER_NECROSIS_POWER.smartDescriptionOnPlayer"
            : "NYMPH_POWER_NECROSIS_POWER.smartDescriptionOnEnemy";

    protected override string RemoteDescriptionLocKey =>
        !IsCanonical && Owner?.IsPlayer == true
            ? "NYMPH_POWER_NECROSIS_POWER.remoteDescriptionOnPlayer"
            : "NYMPH_POWER_NECROSIS_POWER.remoteDescriptionOnEnemy";

    [SavedProperty]
    public int CardsPlayedTowardLayerLoss
    {
        get => _cardsPlayedTowardLayerLoss;
        set
        {
            AssertMutable();
            int clampedValue = Math.Clamp(
                value,
                0,
                CardsPerLayerLoss - 1);
            if (_cardsPlayedTowardLayerLoss == clampedValue)
            {
                return;
            }

            _cardsPlayedTowardLayerLoss = clampedValue;
            DynamicVars["CardsRemaining"].BaseValue =
                CardsPerLayerLoss - clampedValue;
            PowerExtraIconAmountLabelsInvalidated?.Invoke();
        }
    }

    [SavedProperty]
    public int CardsPerLayerLoss
    {
        get => _cardsPerLayerLoss;
        set
        {
            AssertMutable();
            _cardsPerLayerLoss = Math.Max(1, value);
            _cardsPlayedTowardLayerLoss = Math.Min(
                _cardsPlayedTowardLayerLoss,
                _cardsPerLayerLoss - 1);
            DynamicVars["CardsRemaining"].BaseValue =
                _cardsPerLayerLoss - _cardsPlayedTowardLayerLoss;
            PowerExtraIconAmountLabelsInvalidated?.Invoke();
        }
    }

    [SavedProperty]
    public bool ReductionLockedPermanently
    {
        get => _reductionLockedPermanently;
        set
        {
            AssertMutable();
            _reductionLockedPermanently = value;
        }
    }

    [SavedProperty]
    public bool ReductionLockedThisTurn
    {
        get => _reductionLockedThisTurn;
        set
        {
            AssertMutable();
            _reductionLockedThisTurn = value;
        }
    }

    [SavedProperty]
    public int TriggersTowardKeyEffect
    {
        get => _triggersTowardKeyEffect;
        set
        {
            AssertMutable();
            _triggersTowardKeyEffect = Math.Clamp(value, 0, 2);
        }
    }

    private int CardsRemaining =>
        CardsPerLayerLoss - CardsPlayedTowardLayerLoss;

    public static NecrosisPower? GetInstance(
        Creature target,
        Creature? applier)
    {
        return target.GetPowerInstances<NecrosisPower>()
            .FirstOrDefault(power => power.Applier == applier);
    }

    public static int GetTotalAmount(Creature target)
    {
        return target.GetPowerInstances<NecrosisPower>().Sum(power => power.Amount);
    }

    protected override object InitInternalData()
    {
        return new CardPlayData();
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        NecrosisDecayIntervalPower.SyncToNecrosis(Owner);
        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Applier?.Player == null)
        {
            return Task.CompletedTask;
        }

        Player cardOwner = cardPlay.Card.Owner;
        bool isApplierPlayer = cardOwner == Applier.Player;
        bool isLinkedAlly = !isApplierPlayer
            && cardOwner.Creature.GetPower<HeartToHeartPower>() is { } heartToHeart
            && heartToHeart.Applier == Applier;

        if (!isApplierPlayer && !isLinkedAlly)
        {
            return Task.CompletedTask;
        }

        GetInternalData<CardPlayData>().AmountsAtPlayStart[cardPlay.Card] =
            new CardPlayRecord
            {
                AmountAtPlayStart = Amount,
                AdvancesReduction = isApplierPlayer,
            };
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (!GetInternalData<CardPlayData>().AmountsAtPlayStart.Remove(
                cardPlay.Card,
                out CardPlayRecord? record)
            || record is null
            || record.AmountAtPlayStart <= 0
            || Owner.IsDead
            || Applier is null)
        {
            return;
        }

        await Trigger(
            choiceContext,
            Applier,
            cardPlay.Card,
            1,
            record.AmountAtPlayStart,
            advancesReduction: record.AdvancesReduction);
    }

    public async Task Trigger(
        PlayerChoiceContext choiceContext,
        Creature applier,
        CardModel? cardSource,
        int times = 1,
        int? maximumAmount = null,
        bool advancesReduction = true)
    {
        if (times <= 0 || Amount <= 0 || Owner.IsDead)
        {
            return;
        }

        for (int i = 0; i < times; i++)
        {
            int triggerAmount = maximumAmount.HasValue
                ? Math.Min(Amount, maximumAmount.Value)
                : Amount;
            if (triggerAmount <= 0 || Owner.IsDead)
            {
                break;
            }

            Flash();
            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                Owner.HasPower<FearPower>()
                    ? triggerAmount * 2
                    : triggerAmount,
                Owner.Player is null
                    ? DamageProps.nonCardHpLoss
                    : DamageProps.nonCardUnpowered,
                applier);

            // Lethal damage completes the creature's death processing before
            // returning. Do not mutate powers that were just removed with it.
            if (Owner.IsDead)
            {
                return;
            }

            int keyProgress = TriggersTowardKeyEffect + 1;
            if (keyProgress >= 3)
            {
                TriggersTowardKeyEffect = 0;
                await ApplyKeyToHeart(
                    choiceContext,
                    applier,
                    cardSource);
            }
            else
            {
                TriggersTowardKeyEffect = keyProgress;
            }

            if (!advancesReduction
                || ReductionLockedPermanently
                || Owner.HasPower<NecrosisPermanentLockPower>()
                || ReductionLockedThisTurn)
            {
                continue;
            }

            int progress = CardsPlayedTowardLayerLoss + 1;
            if (progress < CardsPerLayerLoss)
            {
                CardsPlayedTowardLayerLoss = progress;
                continue;
            }

            CardsPlayedTowardLayerLoss = 0;
            if (Owner.GetPower<NecrosisReductionBarrierPower>()
                is { Amount: > 0 } barrier)
            {
                await PowerCmd.ModifyAmount(
                    choiceContext,
                    barrier,
                    -1,
                    applier,
                    cardSource);
                continue;
            }

            await PowerCmd.ModifyAmount(
                choiceContext,
                this,
                -1,
                applier,
                cardSource);
        }
    }

    private async Task ApplyKeyToHeart(
        PlayerChoiceContext choiceContext,
        Creature applier,
        CardModel? cardSource)
    {
        int temporaryStrengthLoss = CombatState.Players.Sum(
            player => CombatState
                    .GetOpponentsOf(player.Creature)
                    .Contains(Owner)
                ? player.Creature.GetPower<KeyToHeartPower>()?.Amount ?? 0
                : 0);
        if (temporaryStrengthLoss > 0 && !Owner.IsDead)
        {
            await PowerCmd.Apply<KeyToHeartPower>(
                choiceContext,
                Owner,
                temporaryStrengthLoss,
                applier,
                cardSource);
        }
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Combat.CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (ReductionLockedThisTurn)
        {
            ReductionLockedThisTurn = false;
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs()
    {
        return
        [
            ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.BottomLeft,
                CardsRemaining.ToString()),
        ];
    }

}
