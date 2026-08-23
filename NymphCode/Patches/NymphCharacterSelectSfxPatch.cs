using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using Nymph.Audio;
using Nymph.Characters;

namespace Nymph.Patches;

[HarmonyPatch(
    typeof(NCharacterSelectScreen),
    nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class NymphCharacterSelectSfxPatch
{
    private const string RelicDropMagicalStream =
        "Nymph_Relic_Drop_Magical.ogg";
    private const float Volume = 1.25f;

    [HarmonyPostfix]
    private static void PlayNymphCharacterSelectSfx(CharacterModel characterModel)
    {
        if (characterModel is NymphCharacter)
        {
            NymphAudio.Play(RelicDropMagicalStream, Volume);
            NymphVoiceManager.PlayCharacterSelect();
        }
    }
}
