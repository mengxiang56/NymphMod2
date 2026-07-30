using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Nymph.Mechanics;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NEventRoom), "SetOptions")]
internal static class InspirationNeowPreviewPatch
{
    [HarmonyPostfix]
    private static void FlushAfterNeowIsShown(EventModel eventModel)
    {
        if (eventModel is not Neow)
        {
            return;
        }

        Callable.From(InspirationMechanics.FlushPendingInitialPreviews)
            .CallDeferred();
    }
}
