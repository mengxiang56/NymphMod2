using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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

        Flash();

        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            Owner.HasPower<FearPower>()
                ? amountAtPlayStart * 2
                : amountAtPlayStart,
            DamageProps.nonCardHpLoss,
            cardPlay.Card.Owner.Creature);

        int cardsPlayed = CardsPlayedTowardLayerLoss + 1;
        if (cardsPlayed < CardsPerLayerLoss)
        {
            CardsPlayedTowardLayerLoss = cardsPlayed;
            return;
        }

        CardsPlayedTowardLayerLoss = 0;
        if (Owner.GetPower<NecrosisReductionBarrierPower>()
            is { } reductionBarrier)
        {
            reductionBarrier.Flash();
            await PowerCmd.ModifyAmount(
                choiceContext,
                reductionBarrier,
                -1,
                cardPlay.Card.Owner.Creature,
                cardPlay.Card);
            return;
        }

        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            -1,
            cardPlay.Card.Owner.Creature,
            cardPlay.Card);

        int temporaryStrengthLoss = CombatState.Players.Sum(
            player => CombatState
                    .GetOpponentsOf(player.Creature)
                    .Contains(Owner)
                ? player.Creature
                    .GetPower<KeyToHeartPower>()?.Amount ?? 0
                : 0);
        if (temporaryStrengthLoss > 0 && !Owner.IsDead)
        {
            await PowerCmd.Apply<
                MegaCrit.Sts2.Core.Models.Powers.DarkShacklesPower>(
                choiceContext,
                Owner,
                temporaryStrengthLoss,
                cardPlay.Card.Owner.Creature,
                cardPlay.Card);
        }
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
