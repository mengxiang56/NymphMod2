using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class FreeNarratePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/FreeNarratePower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/FreeNarratePower84.png");

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (Amount <= 0
            || card.Owner != Owner.Player
            || !card.Keywords.Contains(NymphKeywords.Narrate))
        {
            return false;
        }

        modifiedCost = 0;
        return true;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (Amount <= 0
            || cardPlay.Card.Owner != Owner.Player
            || !cardPlay.Card.Keywords.Contains(NymphKeywords.Narrate))
        {
            return;
        }

        Flash();
        await PowerCmd.Decrement(this);
    }
}
