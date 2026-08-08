using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Random;
using Nymph.Characters;

namespace Nymph.Patches;

internal static class NymphMerchantCharacterAnimation
{
    private const string MerchantSceneRootName = "NymphCharacterMerchant";
    private const string MerchantIdleAnim = "Idle";

    internal static Node? FindSpineSprite(NMerchantCharacter merchantCharacter)
    {
        if (!ContainsNodeNamed(merchantCharacter, MerchantSceneRootName))
        {
            return null;
        }

        return FindSpineSpriteRecursive(merchantCharacter);
    }

    internal static void Play(
        NMerchantCharacter merchantCharacter,
        Node spineNode,
        string animation,
        bool loop)
    {
        if (animation == CharacterModel.relaxedAnim)
        {
            animation = MerchantIdleAnim;
        }

        MegaSprite sprite = new(spineNode);
        merchantCharacter.RunWhenSpineReady(sprite, animationState =>
        {
            animationState.SetAnimation(animation, loop);
            if (!loop)
            {
                return;
            }

            using MegaTrackEntry? track = animationState.GetCurrent(0);
            track?.SetTrackTime(
                track.GetAnimationEnd() * Rng.Chaotic.NextFloat());
        });
    }

    private static bool ContainsNodeNamed(Node node, string nodeName)
    {
        if (node.Name.ToString() == nodeName)
        {
            return true;
        }

        foreach (Node child in node.GetChildren())
        {
            if (ContainsNodeNamed(child, nodeName))
            {
                return true;
            }
        }

        return false;
    }

    private static Node? FindSpineSpriteRecursive(Node node)
    {
        if (node.GetClass().ToString() == MegaSprite.spineClassName)
        {
            return node;
        }

        foreach (Node child in node.GetChildren())
        {
            Node? result = FindSpineSpriteRecursive(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}

[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
internal static class NymphMerchantCharacterReadyPatch
{
    [HarmonyPrefix]
    private static bool StartNymphIdle(NMerchantCharacter __instance)
    {
        Node? spineNode =
            NymphMerchantCharacterAnimation.FindSpineSprite(__instance);
        if (spineNode is null)
        {
            return true;
        }

        NymphSkinManager.ApplyCombatSkinToSprite(spineNode);
        NymphMerchantCharacterAnimation.Play(
            __instance,
            spineNode,
            CharacterModel.relaxedAnim,
            loop: true);
        return false;
    }
}

[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
internal static class NymphMerchantCharacterPlayAnimationPatch
{
    [HarmonyPrefix]
    private static bool PlayNymphAnimation(
        NMerchantCharacter __instance,
        string anim,
        bool loop)
    {
        Node? spineNode =
            NymphMerchantCharacterAnimation.FindSpineSprite(__instance);
        if (spineNode is null)
        {
            return true;
        }

        NymphMerchantCharacterAnimation.Play(
            __instance,
            spineNode,
            anim,
            loop);
        return false;
    }
}
