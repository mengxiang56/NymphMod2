using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Nymph.Characters;
using Nymph.Powers;

namespace Nymph.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim))]
internal static class NymphHeartDefenseCollapseAnimationPatch
{
    [HarmonyPrefix]
    private static void UseSkill3Animation(
        Creature creature,
        ref string triggerName)
    {
        if (!creature.IsPlayer
            || creature.Player?.Character is not NymphCharacter
            || creature.GetPower<HeartDefenseCollapsePower>() is null)
        {
            return;
        }

        triggerName = triggerName switch
        {
            "Attack" or "Cast" => NymphCharacter.Skill3AttackTrigger,
            "Hit" or "Idle" => NymphCharacter.Skill3IdleTrigger,
            _ => triggerName,
        };
    }
}
