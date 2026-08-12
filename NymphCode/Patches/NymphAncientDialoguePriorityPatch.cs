using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;

namespace Nymph.Patches;

[HarmonyPatch(
    typeof(AncientDialogueSet),
    nameof(AncientDialogueSet.GetValidDialogues))]
internal static class NymphAncientDialoguePriorityPatch
{
    private static void Prefix(ModelId characterId, ref int totalVisits)
    {
        if (totalVisits == 0
            && characterId == ModelDb.Character<NymphCharacter>().Id)
        {
            // Preserve the character's real visit count while bypassing the
            // profile-wide firstVisitEver dialogue reserved for vanilla introductions.
            totalVisits = 1;
        }
    }
}
