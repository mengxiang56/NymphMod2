using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
    private sealed class CardPlayData
    {
        public Dictionary<CardModel, int> AmountsAtPlayStart { get; } = [];
    }

    private const int DefaultCardsPerLayerLoss = 3;
    private int _cardsPlayedTowardLayerLoss;
    private int _cardsPerLayerLoss = DefaultCardsPerLayerLoss;
    private int _triggersTowardKeyEffect;
    private bool _reductionLockedPermanently;
    private bool _reductionLockedThisTurn;

    public event Action? PowerExtraIconAmountLabelsInvalidated;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardsRemaining", DefaultCardsPerLayerLoss)
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/NymphRelic.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/NymphRelic.png");

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

    public void IncreaseCardsPerLayerLoss(int amount)
    {
        if (amount > 0)
        {
            CardsPerLayerLoss += amount;
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

    protected override object InitInternalData()
    {
        return new CardPlayData();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        GetInternalData<CardPlayData>().AmountsAtPlayStart[cardPlay.Card] = Amount;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (!GetInternalData<CardPlayData>().AmountsAtPlayStart.Remove(
                cardPlay.Card,
                out int amountAtPlayStart)
            || amountAtPlayStart <= 0
            || Owner.IsDead)
        {
            return;
        }

        await Trigger(
            choiceContext,
            cardPlay.Card.Owner.Creature,
            cardPlay.Card,
            1,
            amountAtPlayStart);
    }

    public async Task Trigger(
        PlayerChoiceContext choiceContext,
        Creature applier,
        CardModel? cardSource,
        int times = 1,
        int? maximumAmount = null)
    {
        if (times <= 0 || Amount <= 0 || Owner.IsDead)
        {
            return;
        }

        int multiplier = 1 + CombatState.Players.Sum(
            player => CombatState
                    .GetOpponentsOf(player.Creature)
                    .Contains(Owner)
                ? player.Creature
                    .GetPower<SmallKindnessPower>()?.Amount ?? 0
                : 0);

        for (int i = 0; i < times * multiplier; i++)
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
                DamageProps.nonCardHpLoss,
                applier);

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

            if (ReductionLockedPermanently
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
            await PowerCmd.Apply<
                MegaCrit.Sts2.Core.Models.Powers.DarkShacklesPower>(
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
                ExtraIconAmountLabelCorner.TopLeft,
                CardsRemaining.ToString()),
        ];
    }

}
