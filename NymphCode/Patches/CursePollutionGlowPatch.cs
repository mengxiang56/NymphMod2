using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using Nymph.Mechanics;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NHandCardHolder))]
internal static class CursePollutionGlowPatch
{
    private static readonly Color CurseGlow = new(0.65f, 0.28f, 0.86f, 0.98f);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NHandCardHolder.UpdateCard))]
    private static void UpdateGlow(NHandCardHolder __instance)
    {
        if (!__instance.IsNodeReady() || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        var cardNode = __instance.CardNode;
        if (cardNode?.Model?.Keywords.Contains(NymphKeywords.CursePollution) != true)
        {
            return;
        }

        cardNode.CardHighlight.AnimShow();
        cardNode.CardHighlight.Modulate = CurseGlow;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NHandCardHolder.Flash))]
    private static void RefreshOnKeywordChange(NHandCardHolder __instance)
    {
        __instance.UpdateCard();
    }
}
