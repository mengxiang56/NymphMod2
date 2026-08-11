using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using Nymph.Relics;

namespace Nymph.Patches;

[HarmonyPatch(
    typeof(RelicCmd),
    nameof(RelicCmd.Obtain),
    [typeof(RelicModel), typeof(Player), typeof(int)])]
internal static class NymphBeautifulWishRelicPatch
{
    private static void Postfix(
        Player player,
        ref Task<RelicModel> __result)
    {
        __result = GrantBonusAfterObtain(__result, player);
    }

    private static async Task<RelicModel> GrantBonusAfterObtain(
        Task<RelicModel> originalTask,
        Player player)
    {
        RelicModel obtained = await originalTask;
        NymphBeautifulWishEraRemembrance? wish = player.Relics
            .OfType<NymphBeautifulWishEraRemembrance>()
            .FirstOrDefault();
        if (wish is null || wish.IsGrantingBonus || !wish.TryBeginBonus())
        {
            return obtained;
        }

        try
        {
            RelicModel bonus = RelicFactory
                .PullNextRelicFromFront(player)
                .ToMutable();
            await RelicCmd.Obtain(bonus, player);
        }
        finally
        {
            wish.EndBonus();
        }

        return obtained;
    }
}
