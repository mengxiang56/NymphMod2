using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Random;

namespace Nymph.Patches;

internal static class NymphRestSiteCharacterAnimation
{
    private const string RestSiteSceneRootName = "NymphCharacterRestSite";

    internal static Node? FindSpineSprite(NRestSiteCharacter restSiteCharacter)
    {
        if (!ContainsNodeNamed(restSiteCharacter, RestSiteSceneRootName))
        {
            return null;
        }

        return FindSpineSpriteRecursive(restSiteCharacter);
    }

    internal static void Play(
        NRestSiteCharacter restSiteCharacter,
        Node spineNode,
        string animation,
        bool loop)
    {
        MegaSprite sprite = new(spineNode);
        restSiteCharacter.RunWhenSpineReady(sprite, animationState =>
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

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
internal static class NymphRestSiteCharacterReadyPatch
{
    [HarmonyPostfix]
    private static void StartNymphSit(NRestSiteCharacter __instance)
    {
        Node? spineNode =
            NymphRestSiteCharacterAnimation.FindSpineSprite(__instance);
        if (spineNode is null)
        {
            return;
        }

        NymphRestSiteCharacterAnimation.Play(
            __instance,
            spineNode,
            "Sit",
            loop: true);
    }
}
