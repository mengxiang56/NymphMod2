using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Nymph.Cards;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
internal static class ConstructHistoryFormPortraitPatch
{
    private static void Postfix(NCard __instance)
    {
        if (__instance.Model is NymphConstructHistoryForm)
        {
            ConstructHistoryFormPortraitAnimator.EnsureAttached(__instance);
        }
    }
}

internal static class ConstructHistoryFormPortraitAnimator
{
    private const string AnimatorNodeName = "NymphConstructHistoryFormPortraitAnimator";

    internal static void EnsureAttached(NCard card)
    {
        if (card.GetNodeOrNull(AnimatorNodeName) is not null)
        {
            return;
        }

        card.AddChild(new ConstructHistoryFormPortraitAnimatorNode(card));
    }
}

internal sealed partial class ConstructHistoryFormPortraitAnimatorNode : Node
{
    private readonly NCard _card;

    internal ConstructHistoryFormPortraitAnimatorNode(NCard card)
    {
        _card = card;
        Name = "NymphConstructHistoryFormPortraitAnimator";
    }

    public override void _Process(double delta)
    {
        if (_card.Model is not NymphConstructHistoryForm form)
        {
            QueueFree();
            return;
        }

        if (!form.AdvancePortraitAnimation((float)delta))
        {
            return;
        }

#if STS2_PUBLIC
        _card._portrait.Texture = form.Portrait;
        _card._ancientPortrait.Texture = form.Portrait;
#else
        _card.Call(NCard.MethodName.UpdatePortrait);
#endif
    }
}
