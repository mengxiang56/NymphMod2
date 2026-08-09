using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Nymph.Characters;

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

        __instance.SpineAnimation.SetAnimation("Die", loop: false);
    }
}
