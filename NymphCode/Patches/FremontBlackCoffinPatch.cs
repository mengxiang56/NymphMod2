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
    private static readonly HashSet<CardModel> InternalTransforms = [];

    internal static void BeginInternalTransform(CardModel card) =>
        InternalTransforms.Add(card);

    internal static void EndInternalTransform(CardModel card) =>
        InternalTransforms.Remove(card);

    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__instance is NymphExiledBlackCoffin
            && !InternalTransforms.Contains(__instance))
        {
            __result = false;
        }
    }
}
