using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using Nymph.Audio;
using Nymph.Characters;

namespace Nymph.Patches;

[HarmonyPatch(
    typeof(SfxCmd),
    nameof(SfxCmd.Play),
    [typeof(string), typeof(float)])]
internal static class NymphCustomSfxPatch
{
    private const string AttackStreamName = "Nymph_Attack.ogg";
    private const float AttackVolumeMultiplier = 0.5f;

    [HarmonyPrefix]
    private static bool PlayCustomSfx(string sfx, float volume)
    {
        if (sfx == NymphCharacter.CustomCharacterSelectSfxToken)
        {
            return false;
        }

        string? streamName = sfx switch
        {
            NymphCharacter.CustomAttackSfxToken => AttackStreamName,
            _ => null
        };

        if (streamName is null)
        {
            return true;
        }

        NymphAudio.Play(streamName, volume * AttackVolumeMultiplier);
        return false;
    }
}
