using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using STS2RitsuLib.RunData;

namespace Nymph.Characters;

public enum NymphDifficulty
{
    Standard,
    Heavy,
    Collapse
}

public sealed class NymphDifficultyRunState
{
    public int Difficulty { get; set; }
}

internal static class NymphDifficultyManager
{
    private static readonly string ConfigDirectory = $"user://{Entry.ModId}";
    private static readonly string ConfigPath = $"{ConfigDirectory}/difficulty.cfg";
    private const string ConfigSection = "character";
    private const string ConfigKey = "thought_burden";
    private const string RunDataKey = "thought_burden_difficulty";

    private static readonly DifficultyDefinition[] Difficulties =
    [
        new(
            NymphDifficulty.Standard,
            "NYMPH_DIFFICULTY_SELECT.standard",
            "NYMPH_DIFFICULTY_SELECT.standardDescription"),
        new(
            NymphDifficulty.Heavy,
            "NYMPH_DIFFICULTY_SELECT.heavy",
            "NYMPH_DIFFICULTY_SELECT.heavyDescription"),
        new(
            NymphDifficulty.Collapse,
            "NYMPH_DIFFICULTY_SELECT.collapse",
            "NYMPH_DIFFICULTY_SELECT.collapseDescription"),
    ];

    private static bool _initialized;
    private static int _selectedIndex;
    private static PlayerRunSavedData<NymphDifficultyRunState> _runData = null!;

    internal static int SelectedIndex
    {
        get
        {
            Initialize();
            return _selectedIndex;
        }
    }

    internal static NymphDifficulty Selected =>
        Difficulties[SelectedIndex].Difficulty;

    internal static string SelectedNameKey =>
        Difficulties[SelectedIndex].NameKey;

    internal static string SelectedDescriptionKey =>
        Difficulties[SelectedIndex].DescriptionKey;

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
            _selectedIndex = NormalizeIndex(
                config.GetValue(ConfigSection, ConfigKey, 0).AsInt32());
        }

        _runData = RunSavedDataStore.For(Entry.ModId).RegisterPerPlayer(
            RunDataKey,
            () => new NymphDifficultyRunState
            {
                Difficulty = (int)NymphDifficulty.Standard
            },
            new RunSavedDataOptions
            {
                SyncLobbyOnChange = true
            });
    }

    internal static void Select(int index, StartRunLobby? lobby = null)
    {
        Initialize();
        _selectedIndex = NormalizeIndex(index);
        SaveSelection();

        if (lobby is not null)
        {
            SyncLobby(lobby);
        }
    }

    internal static void SyncLobby(StartRunLobby lobby)
    {
        Initialize();
        _runData.Lobby.Set(
            lobby,
            lobby.LocalPlayer.id,
            new NymphDifficultyRunState
            {
                Difficulty = _selectedIndex
            });
    }

    internal static NymphDifficulty GetFor(Player? player)
    {
        Initialize();
        if (player is null)
        {
            return Selected;
        }

        int index = NormalizeIndex(_runData.Get(player).Difficulty);
        return Difficulties[index].Difficulty;
    }

    private static int NormalizeIndex(int index) =>
        (index % Difficulties.Length + Difficulties.Length)
        % Difficulties.Length;

    private static void SaveSelection()
    {
#pragma warning disable RITSU013
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
            Entry.Logger.Warn($"Unable to save selected Nymph difficulty: {saveError}");
        }
    }

    private sealed record DifficultyDefinition(
        NymphDifficulty Difficulty,
        string NameKey,
        string DescriptionKey);
}
