using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using Nymph.Mechanics;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NOrb))]
internal static class FremontOrbHoverPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnFocus")]
    private static bool OnFocusPrefix(NOrb __instance)
    {
        if (!FremontOrbVisuals.IsFremontOrb(__instance)
            || __instance.Model is null)
        {
            return true;
        }

        LocString description = new(
            "powers", "NYMPH_FREMONT_ORB_HOVER.description");
        description.Add("EvokeDamage", FremontOrbVisuals.GetEvokeDamage(__instance));
        HoverTip hoverTip = new(
            new LocString("powers", "NYMPH_FREMONT_ORB_HOVER.title"),
            description);
        NHoverTipSet.CreateAndShow(
                __instance._bounds,
                hoverTip,
                HoverTip.GetHoverTipAlignment(__instance._bounds))
            ?.SetFollowOwner();
        FremontOrbVisuals.UpdateValueLabels(__instance, isEvoking: false);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnUnfocus")]
    private static void OnUnfocusPostfix(NOrb __instance)
    {
        FremontOrbVisuals.UpdateValueLabels(__instance, isEvoking: false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NOrb.UpdateVisuals))]
    private static void UpdateVisualsPostfix(
        NOrb __instance,
        bool isEvoking)
    {
        FremontOrbVisuals.UpdateValueLabels(__instance, isEvoking);
    }
}
