using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Nymph.Monsters;

namespace Nymph.Patches;

[HarmonyPatch(typeof(AttackCommand), "GetPossibleTargets")]
internal static class TheresaAttackTargetPatch
{
    [HarmonyPostfix]
    private static void ExcludeTheresa(
        AttackCommand __instance,
        ref IReadOnlyList<Creature> __result)
    {
        if (__instance.Attacker?.IsPlayer != true)
        {
            return;
        }

        if (__result.Any(creature => creature.Monster is NymphTheresa))
        {
            __result = __result
                .Where(creature => creature.Monster is not NymphTheresa)
                .ToList();
        }
    }
}
