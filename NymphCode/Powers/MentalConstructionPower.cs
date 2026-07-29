using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
public sealed class MentalConstructionPower : ModPowerTemplate
{
    private bool _usedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/MentalConstructPower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/MentalConstructPower84.png");

    [SavedProperty]
    public bool UsedThisTurn
    {
        get => _usedThisTurn;
        set
        {
            AssertMutable();
            _usedThisTurn = value;
        }
    }

    public async Task OnConceived(
        PlayerChoiceContext choiceContext,
        CardModel source)
    {
        if (UsedThisTurn || source.Owner != Owner.Player)
        {
            return;
        }

        UsedThisTurn = true;
        Flash();
        await RecreateMechanics.SelectFromHandIntoPool(
            choiceContext,
            source,
            0,
            Amount,
            card => card.CanonicalKeywords.Contains(
                NymphKeywords.Narrate));
    }

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner.Player)
        {
            UsedThisTurn = false;
        }

        return Task.CompletedTask;
    }
}
