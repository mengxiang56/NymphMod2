using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using Nymph.Characters;
using Nymph.Compatibility;

namespace Nymph.Patches;

[HarmonyPatch(typeof(NGameOverScreen), nameof(NGameOverScreen.AfterOverlayOpened))]
internal static class NymphGameOverAnimationPatch
{
    [HarmonyPostfix]
    private static void PlayNymphDeathAnimation(NGameOverScreen __instance)
    {
        Control creatureContainer = __instance.GetNode<Control>("%CreatureContainer");
        foreach (Node child in creatureContainer.GetChildren())
        {
            Node? spineNode = child is NCreatureVisuals visuals
                ? NymphSkinManager.FindSpineSprite(visuals)
                : NymphSkinManager.FindSpineSprite(child);
            if (spineNode is null)
            {
                continue;
            }

            MegaSprite spine = new(spineNode);
            if (spine.HasAnimation("Die") && !spine.HasAnimation("die"))
            {
                SpineAnimationCompatibility.SetAnimation(
                    spine.GetAnimationState(),
                    "Die",
                    loop: false);
            }
        }
    }
}
