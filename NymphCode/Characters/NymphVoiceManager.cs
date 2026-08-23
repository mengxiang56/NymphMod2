using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using Nymph.Audio;
using Nymph.Cards;
using STS2RitsuLib.Cards;

namespace Nymph.Characters;

public enum NymphVoiceLanguage
{
    Off,
    Chinese,
    Japanese
}

internal static class NymphVoiceManager
{
    private const string ConfigDirectory = $"user://{Entry.ModId}";
    private const string ConfigPath = $"{ConfigDirectory}/voice.cfg";
    private const string ConfigSection = "character";
    private const string ConfigKey = "voice_language";
    private const float VoiceVolume = 1.5f;

    private static readonly VoiceDefinition[] Voices =
    [
        new(
            NymphVoiceLanguage.Off,
            "NYMPH_VOICE_SELECT.off",
            "NYMPH_VOICE_SELECT.offDescription",
            "Off"),
        new(
            NymphVoiceLanguage.Chinese,
            "NYMPH_VOICE_SELECT.chinese",
            "NYMPH_VOICE_SELECT.chineseDescription",
            "Zhs"),
        new(
            NymphVoiceLanguage.Japanese,
            "NYMPH_VOICE_SELECT.japanese",
            "NYMPH_VOICE_SELECT.japaneseDescription",
            "Jpn"),
    ];

    private static readonly ConditionalWeakTable<PlayerCombatState, object>
        PlayedBattleStarts = new();

    private static bool _initialized;
    private static int _selectedIndex;

    internal static int SelectedIndex
    {
        get
        {
            Initialize();
            return _selectedIndex;
        }
    }

    internal static string SelectedNameKey => Voices[SelectedIndex].NameKey;

    internal static string SelectedDescriptionKey =>
        Voices[SelectedIndex].DescriptionKey;

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

        CardOnPlayHook.RegisterGlobalListener(new VoiceCardPlayListener());
    }

    internal static void Select(int index)
    {
        Initialize();
        _selectedIndex = NormalizeIndex(index);
        SaveSelection();
    }

    internal static void TryPlayBattleStart(Player player)
    {
        if (player.Character is not NymphCharacter
            || !LocalContext.IsMe(player)
            || player.PlayerCombatState is not { TurnNumber: 1 } combatState
            || !PlayedBattleStarts.TryAdd(combatState, new object()))
        {
            return;
        }

        PlayRandom("BattleStart", 3);
    }

    internal static void PlayCharacterSelect()
    {
        VoiceDefinition voice = Voices[SelectedIndex];
        if (voice.Language == NymphVoiceLanguage.Off)
        {
            return;
        }

        NymphAudio.Play(
            $"Nymph_Voice_{voice.FileStem}_CharacterSelect.wav",
            VoiceVolume);
    }

    private static void TryPlayCardVoice(CardModel card)
    {
        if (!LocalContext.IsMine(card)
            || card.Owner.Character is not NymphCharacter
            || card is not (NymphHeartLash
                or NymphHeartDefenseCollapse
                or NymphFearBlast))
        {
            return;
        }

        PlayRandom("Combat", 4);
    }

    private static void PlayRandom(string group, int count)
    {
        VoiceDefinition voice = Voices[SelectedIndex];
        if (voice.Language == NymphVoiceLanguage.Off)
        {
            return;
        }

        int clip = Random.Shared.Next(1, count + 1);
        NymphAudio.Play(
            $"Nymph_Voice_{voice.FileStem}_{group}_{clip}.wav",
            VoiceVolume);
    }

    private static int NormalizeIndex(int index) =>
        (index % Voices.Length + Voices.Length) % Voices.Length;

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
            Entry.Logger.Warn($"Unable to save selected Nymph voice: {saveError}");
        }
    }

    private sealed class VoiceCardPlayListener : ICardOnPlayHookListener
    {
        public Task<bool> BeforeCardOnPlay(BeforeCardOnPlayContext context)
        {
            TryPlayCardVoice(context.CardPlay.Card);
            return Task.FromResult(false);
        }
    }

    private sealed record VoiceDefinition(
        NymphVoiceLanguage Language,
        string NameKey,
        string DescriptionKey,
        string FileStem);
}
