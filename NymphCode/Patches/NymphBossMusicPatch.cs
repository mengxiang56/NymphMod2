using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace Nymph.Patches;

internal static class NymphBossMusic
{
    private static AudioStreamPlayer? _player;

    internal static bool IsPlaying => _player is not null;

    internal static bool TryPlay(NRunMusicController controller, string track)
    {
        string? fileName = track switch
        {
            "nymph:/music/fremont" => "FremontBoss.mp3",
            "nymph:/music/theresis_theresa" => "TheresisTheresaBoss.mp3",
            "nymph:/music/bodrakasti" => "BodrakastiBoss.mp3",
            _ => null
        };
        if (fileName is null)
        {
            return false;
        }

        if (NonInteractiveMode.IsActive)
        {
            return true;
        }

        Stop();
        AudioStreamMP3? stream = ResourceLoader.Load<AudioStreamMP3>(
            $"{Entry.ResPath}/audio/{fileName}");
        if (stream is null)
        {
            Entry.Logger.Warn($"Unable to load boss music: {fileName}");
            controller.PlayCustomMusic("event:/music/act3_boss_queen");
            return true;
        }

        NAudioManager.Instance?.StopMusic();
        stream.Loop = true;
        _player = new AudioStreamPlayer
        {
            Stream = stream,
            ProcessMode = Node.ProcessModeEnum.Always
        };
        UpdateVolume(SaveManager.Instance.SettingsSave.VolumeBgm);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_player);
        _player.Play();
        NAudioManager.Instance?.SetBgmVol(0f);
        CombatManager.Instance.CombatWon += OnCombatWon;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        return true;
    }

    internal static void UpdateVolume(float volume)
    {
        if (_player is not null)
        {
            _player.VolumeDb = volume > 0f
                ? Mathf.LinearToDb(volume * volume)
                : -80f;
        }
    }

    internal static void Stop()
    {
        if (_player is null)
        {
            return;
        }

        CombatManager.Instance.CombatWon -= OnCombatWon;
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        _player.Stop();
        _player.QueueFree();
        _player = null;
        NAudioManager.Instance?.SetBgmVol(
            SaveManager.Instance.SettingsSave.VolumeBgm);
    }

    private static void OnCombatWon(CombatRoom room)
    {
        Stop();
        NRunMusicController.Instance?.StopCustomMusic();
    }

    private static void OnCombatEnded(CombatRoom room) => Stop();
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.PlayCustomMusic))]
internal static class NymphBossMusicStartPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NRunMusicController __instance, string customMusic) =>
        !NymphBossMusic.TryPlay(__instance, customMusic);
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.StopMusic))]
internal static class NymphBossMusicStopPatch
{
    [HarmonyPrefix]
    private static void Prefix() => NymphBossMusic.Stop();
}

[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.SetBgmVol))]
internal static class NymphBossMusicVolumePatch
{
    [HarmonyPrefix]
    private static void Prefix(ref float volume)
    {
        if (NymphBossMusic.IsPlaying)
        {
            volume = 0f;
        }
    }

    [HarmonyPostfix]
    private static void Postfix()
    {
        if (NymphBossMusic.IsPlaying)
        {
            NymphBossMusic.UpdateVolume(
                SaveManager.Instance.SettingsSave.VolumeBgm);
        }
    }
}
