using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using Nymph.Relics;

namespace Nymph.Mechanics;

public static class RecreateRelicMechanics
{
    public static async Task<bool> TryRecreateHeldRelic(
        Player player,
        Func<RelicModel, bool>? relicFilter = null)
    {
        List<RelicModel> eligibleRelics = player.Relics
            .Where(relic =>
                relic.Rarity != RelicRarity.Ancient
                && (relicFilter?.Invoke(relic) ?? true))
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

        RelicModel? fixedReplacement = selected switch
        {
            NymphRelic => ModelDb.Relic<NymphRelic>().ToMutable(),
            NymphThoughtsCatcher =>
                ModelDb.Relic<NymphThoughtsCatcher>().ToMutable(),
            _ => null
        };
        if (fixedReplacement is not null)
        {
            await RelicCmd.Replace(selected, fixedReplacement);
            return true;
        }

        List<RelicModel> candidates = selected.Pool.AllRelics
            .Where(relic =>
                relic.Rarity == selected.Rarity
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
