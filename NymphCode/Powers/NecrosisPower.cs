using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class NecrosisPower :
    ModPowerTemplate,
    IHealthBarForecastSource,
    IPowerExtraIconAmountLabelSpecsProvider,
    IPowerExtraIconAmountLabelsChangeSource
{
    private sealed class CardPlayData
    {
        public Dictionary<CardModel, int> AmountsAtPlayStart { get; } = [];
    }

    private const int CardsPerLayerLoss = 3;
    private static readonly Color ForecastColor =
        new(0.65f, 0.28f, 0.88f, 0.9f);

    private int _cardsPlayedTowardLayerLoss;

    public event Action? PowerExtraIconAmountLabelsInvalidated;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardsRemaining", CardsPerLayerLoss)
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

    public int TotalRemainingHpLoss
    {
        get
        {
            long currentLayerDamage =
                (long)Amount * CardsRemaining;
            long lowerLayerDamage =
                (long)CardsPerLayerLoss
                * Amount
                * (Amount - 1)
                / 2;

            return (int)Math.Min(
                int.MaxValue,
                currentLayerDamage + lowerLayerDamage);
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
            amountAtPlayStart,
            DamageProps.nonCardHpLoss,
            cardPlay.Card.Owner.Creature);

        int cardsPlayed = CardsPlayedTowardLayerLoss + 1;
        if (cardsPlayed < CardsPerLayerLoss)
        {
            CardsPlayedTowardLayerLoss = cardsPlayed;
            return;
        }

        CardsPlayedTowardLayerLoss = 0;
        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            -1,
            cardPlay.Card.Owner.Creature,
            cardPlay.Card);
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

    public IEnumerable<HealthBarForecastSegment>
        GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        return HealthBarForecasts.Single(
            TotalRemainingHpLoss,
            ForecastColor,
            HealthBarForecastGrowthDirection.FromRight);
    }
}
