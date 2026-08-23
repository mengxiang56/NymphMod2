using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Nymph.Mechanics;
using System.Reflection;

namespace Nymph.Patches;

internal static class CursePollutionCardText
{
    internal static void Append(CardModel card, ref string description)
    {
        if (!card.Keywords.Contains(NymphKeywords.CursePollution))
        {
            return;
        }

        LocString extraText = new(
            "card_keywords",
            $"{NymphKeywords.CursePollutionId}.extraCardText");
        description += $"\n[purple]{extraText.GetFormattedText()}[/purple]";
    }
}

[HarmonyPatch]
internal static class CursePollutionDescriptionPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.GetDeclaredMethods(typeof(CardModel))
            .Single(method =>
            {
                ParameterInfo[] parameters = method.GetParameters();
                return method.Name == nameof(CardModel.GetDescriptionForPile)
                    && parameters.Length == 3
                    && parameters[0].ParameterType == typeof(PileType);
            });
    }

    [HarmonyPostfix]
    private static void AddCurseText(CardModel __instance, ref string __result)
    {
        CursePollutionCardText.Append(__instance, ref __result);
    }
}
