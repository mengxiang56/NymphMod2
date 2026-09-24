using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Nymph.Powers;

namespace Nymph.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class HolyCityEmbraceDamagePatch
{
    [HarmonyPostfix]
    private static void ApplyAfterMultipliers(
        Creature? target,
        ModifyDamageHookType modifyDamageHookType,
        ref decimal __result,
        ref IEnumerable<AbstractModel> modifiers)
    {
        if (!modifyDamageHookType.HasFlag(ModifyDamageHookType.Additive)
            || target?.GetPower<HolyCityEmbracePower>() is not { Amount: > 0 } power
            || __result <= 0)
        {
            return;
        }

        __result = Math.Max(0m, __result - power.Amount);
        modifiers = modifiers.Append(power);
    }
}
