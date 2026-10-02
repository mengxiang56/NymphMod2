using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Nymph.Powers;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NPower), "Reload")]
internal static class BossPowerIconSizePatch
{
    private const float IconSize = 46f;

    private static void Postfix(NPower __instance, PowerModel? ____model)
    {
        if (____model is not (
            FremontCardChannelRulePower or
            FremontBlackCoffinRulePower or
            FremontSecondPhaseRulePower or
            TheresisSovereignAfterimagePower or
            TheresisTwinPower or
            TheresisTwinbornPower or
            TheresaWillShockPower or
            BodrakastiHolyCarePower or
            BodrakastiCounselPower or
            BodrakastiGuidancePower or
            BodrakastiGuidanceStrengthLossPower or
            SanctifierAutomatonSelfDestructPower)
            || __instance.GetNodeOrNull<TextureRect>("%Icon") is not { } icon)
        {
            return;
        }

        icon.Size = Vector2.One * IconSize;
        icon.PivotOffset = icon.Size / 2f;
        // Keep the original horizontal center without changing the entry animation's Y position.
        icon.Position = new Vector2((40f - IconSize) / 2f, icon.Position.Y);
    }
}
