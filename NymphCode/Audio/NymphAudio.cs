using Godot;

namespace Nymph.Audio;

internal static class NymphAudio
{
    private const string AudioDirectory = "res://Nymph/audio";

    internal static void Play(string fileName, float linearVolume = 1f)
    {
        AudioStream? stream = ResourceLoader.Load<AudioStream>(
            $"{AudioDirectory}/{fileName}");
        if (stream is null)
        {
            Entry.Logger.Warn($"Unable to load Nymph audio: {fileName}");
            return;
        }

        AudioStreamPlayer player = new()
        {
            Stream = stream,
            ProcessMode = Node.ProcessModeEnum.Always,
            VolumeDb = linearVolume > 0f
                ? Mathf.LinearToDb(linearVolume)
                : -80f
        };
        player.Finished += player.QueueFree;

        SceneTree sceneTree = (SceneTree)Engine.GetMainLoop();
        sceneTree.Root.AddChild(player);
        player.Play();
    }
}
