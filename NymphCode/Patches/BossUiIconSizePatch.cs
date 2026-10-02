using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NBossMapPoint), nameof(NBossMapPoint._Ready))]
internal static class BossMapNodeSizePatch
{
    private static void Postfix(NBossMapPoint __instance)
    {
        if (__instance.GetNodeOrNull<TextureRect>("%PlaceholderImage") is not { } icon
            || icon.Texture?.ResourcePath is not (
                "res://Nymph/images/map/FremontBoss.png" or
                "res://Nymph/images/map/TheresisTheresaBoss.png" or
                "res://Nymph/images/map/BodrakastiBoss.png"))
        {
            return;
        }

        BossUiIconSizing.EnlargeCentered(icon);
    }
}

[HarmonyPatch(typeof(NMapPointHistoryEntry), nameof(NMapPointHistoryEntry._Ready))]
internal static class BossRunHistoryIconSizePatch
{
    private static void Postfix(NMapPointHistoryEntry __instance)
    {
        if (__instance.GetNodeOrNull<TextureRect>("%Icon") is not { } icon
            || icon.Texture?.ResourcePath is not (
                "res://Nymph/images/ui/run_history/FremontBoss_v2.png" or
                "res://Nymph/images/ui/run_history/TheresisTheresaBoss_v2.png" or
                "res://Nymph/images/ui/run_history/BodrakastiBoss_v2.png"))
        {
            return;
        }

        BossUiIconSizing.EnlargeCentered(icon);

        if (__instance.GetNodeOrNull<TextureRect>("%Outline") is { } outline)
        {
            // Our icon and outline use matching canvases; share the same transform.
            outline.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            outline.Scale = Vector2.One;
            outline.PivotOffset = outline.Size / 2f;
        }
    }
}

internal static class BossUiIconSizing
{
    internal const float DisplayScale = 1.2f;

    internal static void EnlargeCentered(TextureRect icon)
    {
        Vector2 oldSize = icon.Size;
        Vector2 oldPosition = icon.Position;
        icon.Size = oldSize * DisplayScale;
        icon.Position = oldPosition + (oldSize - icon.Size) / 2f;
        icon.PivotOffset = icon.Size / 2f;
        // Leave Scale untouched so native hover and entrance tweens still work.
    }
}
