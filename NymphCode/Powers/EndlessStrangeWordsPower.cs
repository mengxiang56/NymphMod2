using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class EndlessStrangeWordsPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/EndlessStoryPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/EndlessStoryPower84.png");

    [SavedProperty]
    public bool ViceVersa { get; set; }

    public void RegisterSource(bool upgraded)
    {
        if (upgraded)
        {
            ViceVersa = true;
        }
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        bool conceived =
            cardPlay.Card.Keywords.Contains(NymphKeywords.Conceive);
        bool narrated = ThoughtMechanics.NarratedAmount(cardPlay) > 0;
        bool narrateRetrieves = narrated && ViceVersa;
        if (!conceived && !narrateRetrieves)
        {
            return;
        }

        int pulls = conceived ? Amount : 0;
        if (narrateRetrieves)
        {
            pulls += Amount * Math.Max(
                1,
                ThoughtMechanics.NarrationEffectCount(cardPlay));
        }

        for (int i = 0; i < pulls; i++)
        {
            List<CardModel> candidates =
            [
                .. PileType.Draw.GetPile(Owner.Player).Cards,
                .. PileType.Discard.GetPile(Owner.Player).Cards
            ];
            candidates = candidates
                .Where(card =>
                    conceived
                        && card.Keywords.Contains(NymphKeywords.Narrate)
                    || narrateRetrieves
                        && card.Keywords.Contains(NymphKeywords.Conceive))
                .Distinct()
                .ToList();
            CardModel? selected = candidates.FirstOrDefault();
            if (selected is null)
            {
                break;
            }

            Flash();
            await CardPileCmd.Add(selected, PileType.Hand);
        }
    }
}
