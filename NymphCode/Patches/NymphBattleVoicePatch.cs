using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using Nymph.Characters;

namespace Nymph.Patches;

[HarmonyPatch(
    typeof(CombatManager),
    nameof(CombatManager.RunAutoPrePlayPhase))]
internal static class NymphBattleVoicePatch
{
    [HarmonyPrefix]
    private static void PlayBattleStartVoice(Player player)
    {
        NymphVoiceManager.TryPlayBattleStart(player);
    }
}
