using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using Nymph.Mechanics;
using STS2RitsuLib.CardPiles.Nodes;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NModCardPileButton), "OnMouseEntered")]
internal static class InspirationPileHoverTipPatch
{
    private static readonly FieldInfo? ActiveHoverTipsField =
        AccessTools.Field(typeof(NHoverTipSet), "_activeHoverTips");

    [HarmonyPostfix]
    private static void RepositionHoverTip(NModCardPileButton __instance)
    {
        if (__instance.Definition?.Id != InspirationMechanics.PileId
            || ActiveHoverTipsField?.GetValue(null)
                is not Dictionary<Control, NHoverTipSet> activeTips
            || !activeTips.TryGetValue(__instance, out NHoverTipSet? tipSet)
            || tipSet is null)
        {
            return;
        }

        tipSet.GlobalPosition = __instance.GlobalPosition
            + new Vector2(__instance.Size.X - tipSet.Size.X, __instance.Size.Y + 20f);
    }
}
