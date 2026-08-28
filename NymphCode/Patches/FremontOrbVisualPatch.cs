using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Nymph.Mechanics;
using Nymph.Monsters;
using Nymph.Powers;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
internal static class FremontOrbVisualPatch
{
    private static void Postfix(Creature creature)
    {
        if (creature.Monster is not Fremont)
        {
            return;
        }

        int orbCount = creature.GetPower<FremontMechanicsPower>()?.OrbCount ?? 0;
        FremontOrbVisuals.Sync(
            creature,
            orbCount,
            animateNewOrbs: false);
    }
}
