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

    private static void Postfix(
        AncientDialogueSet __instance,
        ModelId characterId,
        ref IEnumerable<AncientDialogue> __result)
    {
        if (characterId != ModelDb.Character<NymphCharacter>().Id
            || !__instance.CharacterDialogues.TryGetValue(
                characterId.Entry,
                out IReadOnlyList<AncientDialogue>? nymphDialogues))
        {
            return;
        }

        List<AncientDialogue> resolved = __result.ToList();
        List<AncientDialogue> characterSpecific = resolved
            .Where(nymphDialogues.Contains)
            .ToList();
        if (characterSpecific.Count > 0)
        {
            __result = characterSpecific;
        }
    }
}
