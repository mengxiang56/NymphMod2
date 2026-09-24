using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Nymph.Monsters;
using Nymph.Powers;

namespace Nymph.Patches;

[HarmonyPatch(typeof(DamageVar), nameof(DamageVar.UpdateCardPreview))]
internal static class BodrakastiDamagePreviewPatch
{
    private static void Postfix(
        DamageVar __instance,
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        bool runGlobalHooks)
    {
        if (!runGlobalHooks || target?.Monster is not Bodrakasti)
        {
            return;
        }

        if (card.Owner.Creature == target)
        {
            return;
        }

        int embrace = target.GetPower<BodrakastiHolyCityEmbracePower>()
            ?.Amount ?? 0;
        __instance.PreviewValue = Math.Max(
            0m,
            __instance.PreviewValue - embrace);
    }
}
