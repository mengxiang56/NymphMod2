using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using Nymph.Encounters;

namespace Nymph.Patches;

// Temporary test override: always use the newest custom boss in Act 1.
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class TheresisTheresaActOneRoomGenerationPatch
{
    private static void Postfix(ActModel __instance)
    {
        SetTestBoss(__instance);
    }

    internal static void SetTestBoss(ActModel act)
    {
        if (act is Overgrowth)
        {
            act.SetBossEncounter(
                ModelDb.Encounter<BodrakastiBoss>());
        }
    }
}

[HarmonyPatch(
    typeof(ActModel),
    nameof(ActModel.ApplyDiscoveryOrderModifications))]
internal static class TheresisTheresaActOneDiscoveryOrderPatch
{
    private static void Postfix(ActModel __instance)
    {
        TheresisTheresaActOneRoomGenerationPatch.SetTestBoss(__instance);
    }
}
