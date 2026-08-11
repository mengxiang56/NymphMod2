using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class ImpressionReconstructionPower : ModPowerTemplate
{
    private readonly HashSet<CardModel> _discountedCards = [];
    private int _usedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/ImpressionReconstructionPower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/ImpressionReconstructionPower84.png");

    [SavedProperty]
    public int UsedThisTurn
    {
        get => _usedThisTurn;
        set
        {
            AssertMutable();
            _usedThisTurn = Math.Max(0, value);
        }
    }

    public static void ReduceRecreatedCardCosts(
        Player owner,
        IReadOnlyList<RecreateResult> results)
    {
        ImpressionReconstructionPower? power =
            owner.Creature.GetPower<ImpressionReconstructionPower>();
        if (power is null)
        {
            return;
        }

        int remaining = Math.Max(0, power.Amount - power.UsedThisTurn);
        foreach (RecreateResult result in results.Take(remaining))
        {
            CardModel card = result.Replacement;
            card.AssertMutable();
            power._discountedCards.Add(card);
            card.InvokeEnergyCostChanged();
            power.UsedThisTurn++;
            power.Flash();
        }
    }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != Owner.Player
            || !_discountedCards.Contains(card))
        {
            return false;
        }

        modifiedCost = Math.Max(0, originalCost - 1);
        return true;
    }

    public override Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        _discountedCards.Remove(cardPlay.Card);
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner.Player)
        {
            UsedThisTurn = 0;
            _discountedCards.Clear();
        }

        return Task.CompletedTask;
    }
}
