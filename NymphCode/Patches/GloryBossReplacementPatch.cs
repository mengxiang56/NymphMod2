using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using Nymph.Characters;
using Nymph.Encounters;

namespace Nymph.Patches;

[HarmonyPatch(
    typeof(ActModel),
    nameof(ActModel.AllBossEncounters),
    MethodType.Getter)]
internal static class GloryBossPoolPatch
{
    internal static bool ShouldReplaceBosses(ActModel act) =>
        act is Glory &&
        RunManager.Instance.State?.Players.Any(player =>
            player.Character is NymphCharacter &&
            NymphBossSelectionManager.GetFor(player)) == true;

    private static void Postfix(
        ActModel __instance,
        ref IEnumerable<EncounterModel> __result)
    {
        if (__instance is not Glory)
        {
            return;
        }

        if (!ShouldReplaceBosses(__instance))
        {
            // These encounters are also registered in Glory's shared pool.
            __result = __result.Where(encounter =>
                encounter is not (FremontBoss or TheresisTheresaBoss or BodrakastiBoss));
            return;
        }

        __result =
        [
            ModelDb.Encounter<FremontBoss>(),
            ModelDb.Encounter<TheresisTheresaBoss>(),
            ModelDb.Encounter<BodrakastiBoss>()
        ];
    }
}

[HarmonyPatch(
    typeof(ActModel),
    nameof(ActModel.ApplyDiscoveryOrderModifications))]
internal static class GloryBossDiscoveryOrderPatch
{
    private static bool Prefix(ActModel __instance) =>
        !GloryBossPoolPatch.ShouldReplaceBosses(__instance);
}
