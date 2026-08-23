using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Nymph.Cards;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class CursePollutionThisTurnPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        CardModel card = cardPlay.Card;
        if (card.Owner != Owner.Player
            || !card.Keywords.Contains(NymphKeywords.CursePollution))
        {
            return;
        }

        CardCmd.RemoveKeyword(card, NymphKeywords.CursePollution);
        CardModel dreadkaz = card.CombatState!.CreateCard<NymphDreadkaz>(
            card.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            dreadkaz,
            PileType.Hand,
            card.Owner);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        Player? player = Owner.Player;
        if (!participants.Contains(Owner) || player is null)
        {
            return;
        }

        foreach (CardModel card in player.PlayerCombatState!.AllCards
                     .Where(card => card.Keywords.Contains(
                         NymphKeywords.CursePollution))
                     .ToList())
        {
            CardCmd.RemoveKeyword(card, NymphKeywords.CursePollution);
        }

        await PowerCmd.Remove(this);
    }
}
