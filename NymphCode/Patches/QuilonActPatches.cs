using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using Nymph.Acts;
using Nymph.Settings;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NGame), nameof(NGame.StartNewSingleplayerRun))]
internal static class AppendQuilonActPatch
{
    private static void Prefix(ref IReadOnlyList<ActModel> acts, GameMode gameMode)
    {
        if (!NymphBossSettings.EnableQuilonBoss || gameMode != GameMode.Standard || acts.Any(a => a is QuilonAct))
        {
            return;
        }

        List<ActModel> expanded = acts.ToList();
        expanded.Add(ModelDb.Act<QuilonAct>());
        acts = expanded;
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.CreateMap))]
internal static class QuilonActMapPatch
{
    private static void Postfix(ActModel __instance, ref ActMap __result)
    {
        if (__instance is QuilonAct)
        {
            __result = new QuilonActMap();
        }
    }
}
