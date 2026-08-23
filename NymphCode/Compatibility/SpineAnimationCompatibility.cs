using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace Nymph.Compatibility;

internal static class SpineAnimationCompatibility
{
    internal static void SetAnimation(
        MegaAnimationState? animationState,
        string animation,
        bool loop,
        int track = 0)
    {
        animationState?.Call("set_animation", animation, loop, track);
    }
}
