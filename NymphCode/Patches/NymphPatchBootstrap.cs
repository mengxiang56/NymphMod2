using System.Reflection;
using HarmonyLib;

namespace Nymph.Patches;

internal static class NymphPatchBootstrap
{
    private static bool _isApplied;

    public static void Apply()
    {
        if (_isApplied)
        {
            return;
        }

        new Harmony($"{Entry.ModId}.Patches")
            .PatchAll(Assembly.GetExecutingAssembly());
        _isApplied = true;
    }
}
