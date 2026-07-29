using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Nymph.Characters;

namespace Nymph.Mechanics;

public static class DiscoveryMechanics
{
    public static async Task Discover(
        PlayerChoiceContext choiceContext,
        Player player,
        bool upgraded)
    {
        List<CardModel> options = CardFactory.GetDistinctForCombat(
                player,
                ModelDb.CardPool<NymphCardPool>()
                    .GetUnlockedCards(
                        player.UnlockState,
                        player.RunState.CardMultiplayerConstraint),
                3,
                player.RunState.Rng.CombatCardGeneration)
            .ToList();

        if (upgraded)
        {
            foreach (CardModel option in options.Where(card => card.IsUpgradable))
            {
                CardCmd.Upgrade(option, CardPreviewStyle.None);
            }
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            options,
            player,
            canSkip: true);
        if (selected is null)
        {
            return;
        }

        selected.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(
            selected,
            PileType.Hand,
            player);
    }
}
