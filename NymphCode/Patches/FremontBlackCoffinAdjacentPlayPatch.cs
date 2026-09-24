using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Nymph.Cards;
using Nymph.Powers;

namespace Nymph.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class FremontBlackCoffinAdjacentPlayPatch
{
    private static void Prefix(
        CardModel __instance,
        out List<NymphExiledBlackCoffin> __state)
    {
        __state = [];
        if (__instance.Pile?.Type != PileType.Hand)
        {
            return;
        }

        List<CardModel> hand = PileType.Hand.GetPile(__instance.Owner)
            .Cards.ToList();
        int index = hand.IndexOf(__instance);
        if (index < 0)
        {
            return;
        }

        if (index > 0
            && hand[index - 1] is NymphExiledBlackCoffin left)
        {
            __state.Add(left);
        }

        if (index + 1 < hand.Count
            && hand[index + 1] is NymphExiledBlackCoffin right)
        {
            __state.Add(right);
        }
    }

    private static void Postfix(
        CardModel __instance,
        ref Task __result,
        List<NymphExiledBlackCoffin> __state)
    {
        bool facingFremont = __instance.Owner.Creature.CombatState?.Enemies
            .Any(enemy => enemy.GetPower<FremontMechanicsPower>() is not null)
            == true;
        if (__state.Count > 0 || facingFremont)
        {
            __result = CompletePlay(__result, __instance, __state);
        }
    }

    private static async Task CompletePlay(
        Task play,
        CardModel card,
        List<NymphExiledBlackCoffin> coffins)
    {
        await play;
        await FremontMechanicsPower.TransformMarkedCard(card);
        foreach (NymphExiledBlackCoffin coffin in coffins)
        {
            await coffin.RestoreOriginal();
        }
    }
}
