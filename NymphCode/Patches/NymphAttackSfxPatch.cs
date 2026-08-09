using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using Nymph.Characters;

namespace Nymph.Patches;

[HarmonyPatch(
    typeof(SfxCmd),
    nameof(SfxCmd.Play),
    [typeof(string), typeof(float)])]
internal static class NymphAttackSfxPatch
{
    private const string AttackStreamName = "Nymph_Attack.ogg";

    [HarmonyPrefix]
    private static bool PlayCustomAttackSfx(string sfx, float volume)
    {
        if (sfx != NymphCharacter.CustomAttackSfxToken)
        {
            return true;
        }

        NDebugAudioManager.Instance?.Play(AttackStreamName, volume);
        return false;
    }
}
