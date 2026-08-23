using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Nymph.Characters;
using Nymph.Compatibility;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
internal static class NymphDeathAnimationPatch
{
    [HarmonyPrefix]
    private static void PlayNymphDeathAnimation(NCreature __instance)
    {
        if (__instance.Entity.Player?.Character is not NymphCharacter
            || !__instance.HasSpineAnimation)
        {
            return;
        }

        SpineAnimationCompatibility.SetAnimation(
            __instance.SpineAnimation.GetAnimationState(),
            "Die",
            loop: false);
    }
}
