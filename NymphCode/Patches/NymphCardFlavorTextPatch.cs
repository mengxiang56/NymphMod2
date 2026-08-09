using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Nymph.Patches;

internal static class NymphCardFlavorText
{
    internal const string HoverTipIdPrefix = "nymph-card-flavor:";

    internal static bool TryCreate(CardModel card, out HoverTip flavorTip)
    {
        string entry = card.Id.Entry;
        string flavorKey = $"{entry}.flavor";
        if (!entry.StartsWith("NYMPH_CARD_", StringComparison.Ordinal)
            || !LocManager.Instance.GetTable("cards").HasEntry(flavorKey))
        {
            flavorTip = default;
            return false;
        }

        flavorTip = new HoverTip(new LocString("cards", flavorKey))
        {
            Id = $"{HoverTipIdPrefix}{card.Id}"
        };
        return true;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
internal static class NymphCardFlavorTextPatch
{
    [HarmonyPostfix]
    private static void AddFlavorText(
        CardModel __instance,
        ref IEnumerable<IHoverTip> __result)
    {
        if (!NymphCardFlavorText.TryCreate(__instance, out HoverTip flavorTip))
        {
            return;
        }

        __result = __result.Append(flavorTip);
    }
}

[HarmonyPatch(typeof(NHoverTipSet), "Init")]
internal static class NymphCardFlavorTextStylePatch
{
    private static readonly Color FlavorBackgroundColor =
        new(0.66f, 0.28f, 0.08f, 1f);

    [HarmonyPostfix]
    private static void StyleFlavorText(
        NHoverTipSet __instance,
        IEnumerable<IHoverTip> hoverTips)
    {
        List<HoverTip> textTips = IHoverTip.RemoveDupes(hoverTips)
            .OfType<HoverTip>()
            .ToList();
        VFlowContainer? textContainer = __instance.GetChildren()
            .OfType<VFlowContainer>()
            .FirstOrDefault();
        if (textContainer is null)
        {
            return;
        }

        List<Control> tipNodes = textContainer.GetChildren()
            .OfType<Control>()
            .ToList();
        int count = Math.Min(textTips.Count, tipNodes.Count);
        for (int i = 0; i < count; i++)
        {
            if (!textTips[i].Id.StartsWith(
                    NymphCardFlavorText.HoverTipIdPrefix,
                    StringComparison.Ordinal))
            {
                continue;
            }

            tipNodes[i].GetNode<CanvasItem>("%Bg").SelfModulate =
                FlavorBackgroundColor;
        }
    }
}
