using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using Nymph.Encounters;

namespace Nymph.Patches;

[HarmonyPatch(
    typeof(ActModel),
    nameof(ActModel.AllBossEncounters),
    MethodType.Getter)]
internal static class GloryBossPoolPatch
{
    private static void Postfix(
        ActModel __instance,
        ref IEnumerable<EncounterModel> __result)
    {
        if (__instance is not Glory)
        {
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
        __instance is not Glory;

    private static void Postfix(ActModel __instance)
    {
        ActOneTestBossPatch.SetTestBoss(__instance);
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class ActOneTestBossPatch
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
                ModelDb.Encounter<TheresisTheresaBoss>());
        }
        else if (act is Underdocks)
        {
            act.SetBossEncounter(
                ModelDb.Encounter<BodrakastiBoss>());
        }
    }
}
