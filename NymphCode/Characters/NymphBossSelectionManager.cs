using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using STS2RitsuLib.RunData;

namespace Nymph.Characters;

public sealed class NymphBossSelectionRunState
{
    public bool Enabled { get; set; }
}

internal static class NymphBossSelectionManager
{
    private static readonly string ConfigDirectory = $"user://{Entry.ModId}";
    private static readonly string ConfigPath = $"{ConfigDirectory}/boss_selection.cfg";
    private const string ConfigSection = "character";
    private const string ConfigKey = "replace_act_three_bosses";
    private static bool _initialized;
    private static bool _selected;
    private static PlayerRunSavedData<NymphBossSelectionRunState> _runData = null!;

    internal static bool Selected
    {
        get
        {
            Initialize();
            return _selected;
        }
    }

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
            _selected = config.GetValue(ConfigSection, ConfigKey, false).AsBool();
        }

        _runData = RunSavedDataStore.For(Entry.ModId).RegisterPerPlayer(
            ConfigKey,
            () => new NymphBossSelectionRunState(),
            new RunSavedDataOptions
            {
                SyncLobbyOnChange = true
            });
    }

    internal static void Select(bool enabled, StartRunLobby? lobby = null)
    {
        Initialize();
        _selected = enabled;
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
            new NymphBossSelectionRunState { Enabled = _selected });
    }

    internal static bool GetFor(Player player)
    {
        Initialize();
        return _runData.Get(player).Enabled;
    }

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
        config.SetValue(ConfigSection, ConfigKey, _selected);
        Error saveError = config.Save(ConfigPath);
        if (saveError != Error.Ok)
        {
            Entry.Logger.Warn($"Unable to save selected Nymph bosses: {saveError}");
        }
    }
}
