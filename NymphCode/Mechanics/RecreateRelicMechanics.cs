using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace Nymph.Mechanics;

public static class RecreateRelicMechanics
{
    public static async Task<bool> TryRecreateHeldRelic(Player player)
    {
        List<RelicModel> eligibleRelics = player.Relics
            .Where(relic => relic.Rarity != RelicRarity.Ancient)
            .ToList();
        if (eligibleRelics.Count == 0)
        {
            return false;
        }

        RelicModel? selected = await RelicSelectCmd.FromChooseARelicScreen(
            player,
            eligibleRelics);
        if (selected is null)
        {
            return false;
        }

        List<RelicModel> candidates = selected.Pool.AllRelics
            .Where(relic =>
                relic.Rarity != RelicRarity.Ancient
                && relic.Id != selected.Id
                && player.Relics.All(owned => owned.Id != relic.Id))
            .ToList();
        if (candidates.Count == 0)
        {
            return false;
        }

        RelicModel replacement = player.PlayerRng.Rewards
            .NextItem(candidates)!
            .ToMutable();
        await RelicCmd.Replace(selected, replacement);
        return true;
    }
}
