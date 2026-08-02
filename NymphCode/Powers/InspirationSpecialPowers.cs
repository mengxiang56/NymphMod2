using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class InspirationCityDrivingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/DispileCardToHandPower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/DispileCardToHandPower84.png");

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        CardPile discard = PileType.Discard.GetPile(player);
        if (discard.Cards.Count == 0)
        {
            return;
        }

        CardSelectorPrefs prefs = new(
            new LocString(
                "powers",
                "NYMPH_POWER_INSPIRATION_CITY_DRIVING_POWER.selectionPrompt"),
            Amount);
        IReadOnlyList<CardModel> selected = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            discard,
            player,
            prefs)).ToList();
        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }

        if (selected.Count > 0)
        {
            Flash();
        }
    }
}

[RegisterPower]
public sealed class InspirationTemperBladePower : ModPowerTemplate
{
    private bool _triggeredThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/TemperBladePower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/TemperBladePower84.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>()
    ];

    [SavedProperty]
    public bool TriggeredThisTurn
    {
        get => _triggeredThisTurn;
        set
        {
            AssertMutable();
            _triggeredThisTurn = value;
        }
    }

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner.Player)
        {
            TriggeredThisTurn = false;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterAttack(
        PlayerChoiceContext choiceContext,
        AttackCommand command)
    {
        if (TriggeredThisTurn
            || command.Attacker != Owner
            || command.ModelSource is not CardModel { Type: CardType.Attack })
        {
            return;
        }

        var target = command.Results
            .SelectMany(result => result)
            .Select(result => result.Receiver)
            .FirstOrDefault(creature => creature != Owner);
        if (target is null)
        {
            return;
        }

        TriggeredThisTurn = true;
        if (target.IsDead)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<NecrosisPower>(
            choiceContext,
            target,
            Amount,
            Owner,
            command.ModelSource as CardModel);
    }
}
