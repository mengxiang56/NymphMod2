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

        HoverTip hoverTip = new(
            new LocString("powers", "NYMPH_FREMONT_ORB_HOVER.title"),
            new LocString("powers", "NYMPH_FREMONT_ORB_HOVER.description"));
        NHoverTipSet.CreateAndShow(
                __instance._bounds,
                hoverTip,
                HoverTip.GetHoverTipAlignment(__instance._bounds))
            ?.SetFollowOwner();
        __instance._labelContainer.Visible = false;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnUnfocus")]
    private static void OnUnfocusPostfix(NOrb __instance)
    {
        HideBaseValues(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NOrb.UpdateVisuals))]
    private static void UpdateVisualsPostfix(NOrb __instance)
    {
        HideBaseValues(__instance);
    }

    private static void HideBaseValues(NOrb orb)
    {
        if (FremontOrbVisuals.IsFremontOrb(orb))
        {
            orb._labelContainer.Visible = false;
        }
    }
}
