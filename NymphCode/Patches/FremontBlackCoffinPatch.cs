using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Nymph.Cards;

namespace Nymph.Patches;

[HarmonyPatch(
    typeof(CardModel),
    nameof(CardModel.IsTransformable),
    MethodType.Getter)]
internal static class FremontBlackCoffinPatch
{
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__instance is NymphExiledBlackCoffin)
        {
            __result = false;
        }
    }
}
