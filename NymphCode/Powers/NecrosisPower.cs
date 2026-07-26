using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class NecrosisPower : ModPowerTemplate, IPowerExtraIconAmountLabelSpecsProvider
{
    private sealed class CardPlayData
    {
        public Dictionary<CardModel, int> AmountsAtPlayStart { get; } = [];
    }

    private const int CardsPerLayerLoss = 3;

    private int _cardsPlayedTowardLayerLoss;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

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
            _cardsPlayedTowardLayerLoss = Math.Clamp(value, 0, CardsPerLayerLoss);
        }
    }

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
            await PowerCmd.ModifyAmount(
                choiceContext,
                this,
                0,
                cardPlay.Card.Owner.Creature,
                cardPlay.Card,
                true);
            return;
        }

        CardsPlayedTowardLayerLoss = 0;
        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            0,
            cardPlay.Card.Owner.Creature,
            cardPlay.Card,
            true);
        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            -1,
            cardPlay.Card.Owner.Creature,
            cardPlay.Card);
    }

    public IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs()
    {
        int cardsRemaining = CardsPerLayerLoss - CardsPlayedTowardLayerLoss;
        return
        [
            // 纯文本
            ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.TopLeft,
                cardsRemaining.ToString()),
        ];
    }
}
