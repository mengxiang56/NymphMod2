using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace Nymph.Characters;

internal static class NymphSkinManager
{
    private static readonly string ConfigDirectory = $"user://{Entry.ModId}";
    private static readonly string ConfigPath = $"{ConfigDirectory}/appearance.cfg";
    private const string ConfigSection = "character";
    private const string ConfigKey = "skin";

    private static readonly SkinDefinition[] Skins =
    [
        new(
            "NYMPH_SKIN_SELECT.default",
            $"{Entry.ResPath}/images/characters/skin0/char_4146_nymph_skeleton_data.tres",
            $"{Entry.ResPath}/images/characters/skin0/build_char_4146_nymph_skeleton_data.tres",
            $"{Entry.ResPath}/images/characters/skin0/Nymph_Portrait0.png"),
        new(
            "NYMPH_SKIN_SELECT.epoque",
            $"{Entry.ResPath}/images/characters/skin1/char_4146_nymph_epoque_42_skeleton_data.tres",
            $"{Entry.ResPath}/images/characters/skin1/build_char_4146_nymph_epoque_42_skeleton_data.tres",
            $"{Entry.ResPath}/images/characters/skin1/Nymph_Portrait1.png"),
        new(
            "NYMPH_SKIN_SELECT.ambienceSynesthesia",
            $"{Entry.ResPath}/images/characters/skin2/char_4146_nymph_ambience_synesthesia_skeleton_data.tres",
            $"{Entry.ResPath}/images/characters/skin2/build_char_4146_nymph_ambience_synesthesia_skeleton_data.tres",
            $"{Entry.ResPath}/images/characters/skin2/Nymph_Portrait2.png"),
    ];

    private static bool _initialized;
    private static int _selectedIndex;

    internal static int Count => Skins.Length;

    internal static int SelectedIndex
    {
        get
        {
            Initialize();
            return _selectedIndex;
        }
    }

    internal static string SelectedNameKey => Skins[SelectedIndex].NameKey;

    internal static string SelectedBackgroundPath =>
        Skins[SelectedIndex].CharacterSelectBackgroundPath;

    internal static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ConfigFile config = new();
        if (config.Load(ConfigPath) == Error.Ok)
        {
            int savedIndex = config.GetValue(ConfigSection, ConfigKey, 0).AsInt32();
            _selectedIndex = Math.Clamp(savedIndex, 0, Skins.Length - 1);
        }
    }

    internal static void Select(int index)
    {
        Initialize();
        _selectedIndex = (index % Skins.Length + Skins.Length) % Skins.Length;

#pragma warning disable RITSU013 // user:// is writable runtime data, not a packaged resource.
        string absoluteDirectory = ProjectSettings.GlobalizePath(ConfigDirectory);
#pragma warning restore RITSU013
        Error directoryError = DirAccess.MakeDirRecursiveAbsolute(absoluteDirectory);
        if (directoryError != Error.Ok && directoryError != Error.AlreadyExists)
        {
            Entry.Logger.Warn($"Unable to create Nymph config directory: {directoryError}");
            return;
        }

        ConfigFile config = new();
        config.SetValue(ConfigSection, ConfigKey, _selectedIndex);
        Error saveError = config.Save(ConfigPath);
        if (saveError != Error.Ok)
        {
            Entry.Logger.Warn($"Unable to save selected Nymph skin: {saveError}");
        }
    }

    internal static bool ApplyCombatSkin(Node root) =>
        ApplySkin(root, Skins[SelectedIndex].CombatSkeletonPath);

    internal static bool ApplyRestSiteSkin(Node root) =>
        ApplySkin(root, Skins[SelectedIndex].RestSiteSkeletonPath);

    internal static bool ApplyCombatSkinToSprite(Node spineNode) =>
        ApplySkinToSprite(spineNode, Skins[SelectedIndex].CombatSkeletonPath);

    internal static bool ApplyRestSiteSkinToSprite(Node spineNode) =>
        ApplySkinToSprite(spineNode, Skins[SelectedIndex].RestSiteSkeletonPath);

    internal static Node? FindSpineSprite(Node node)
    {
        if (node.GetClass().ToString() == MegaSprite.spineClassName)
        {
            return node;
        }

        foreach (Node child in node.GetChildren())
        {
            Node? result = FindSpineSprite(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static bool ApplySkin(Node root, string skeletonPath)
    {
        Node? spineNode = FindSpineSprite(root);
        return spineNode is not null && ApplySkinToSprite(spineNode, skeletonPath);
    }

    private static bool ApplySkinToSprite(Node spineNode, string skeletonPath)
    {
        Resource? skeletonData = ResourceLoader.Load<Resource>(
            skeletonPath,
            null,
            ResourceLoader.CacheMode.Reuse);
        if (skeletonData is null)
        {
            Entry.Logger.Error($"Unable to load Nymph skin skeleton: {skeletonPath}");
            return false;
        }

        MegaSprite sprite = new(spineNode);
        MegaSkeletonDataResource data = new(Variant.From(skeletonData));
        sprite.SetSkeletonDataRes(data);
        return true;
    }

    private sealed record SkinDefinition(
        string NameKey,
        string CombatSkeletonPath,
        string RestSiteSkeletonPath,
        string CharacterSelectBackgroundPath);
}
