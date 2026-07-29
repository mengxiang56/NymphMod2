using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.addons.mega_text;
using Nymph.Mechanics;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NCardPileScreen), nameof(NCardPileScreen._Ready))]
internal static class InspirationPileScreenPatch
{
    private static bool _isApplied;

    public static void Apply()
    {
        if (_isApplied)
        {
            return;
        }

        new Harmony($"{Entry.ModId}.InspirationPileScreen")
            .CreateClassProcessor(typeof(InspirationPileScreenPatch))
            .Patch();
        _isApplied = true;
    }

    [HarmonyPostfix]
    private static void ShowDescription(NCardPileScreen __instance)
    {
        if (__instance.Pile.Type != InspirationMechanics.PileType)
        {
            return;
        }

        MegaRichTextLabel bottomLabel =
            __instance.GetNode<MegaRichTextLabel>("%BottomLabel");
        bottomLabel.Text =
            "[center]"
            + new LocString(
                "static_hover_tips",
                $"{InspirationMechanics.PileId}.description")
                .GetFormattedText();
        bottomLabel.Visible = true;
    }
}
