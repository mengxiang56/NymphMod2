using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Random;
using Nymph.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Encounters;

[RegisterActEncounter(typeof(Glory))]
public sealed class FremontBoss : ModEncounterTemplate
{
    private const string MapNodePath = "res://Nymph/images/map/FremontBoss";
    private const string RunHistoryIconPath =
        "res://Nymph/images/ui/run_history/FremontBoss_v2.png";
    private const string RunHistoryOutlinePath =
        "res://Nymph/images/ui/run_history/FremontBoss_v2_outline.png";

    public override RoomType RoomType => RoomType.Boss;
    protected override bool UseProgrammaticCombatBackground => true;

    protected override BackgroundAssets? BuildProgrammaticCombatBackground(ActModel parentAct, Rng rng) =>
        CombatBackgroundAssetsFactory.Create(
            "res://Nymph/scenes/backgrounds/boss_combat_background.tscn",
            ["res://Nymph/scenes/backgrounds/fremont_bg_00_a.tscn"]);

    public override string CustomBgm => "nymph:/music/fremont";
    // The native PNG fallback uses a prefix, not an existing Spine resource.
#pragma warning disable RITSU013
    public override string BossNodePath =>
        MapNodePath;
#pragma warning restore RITSU013
    public override IEnumerable<string>? CustomMapNodeAssetPaths =>
        [MapNodePath + ".png", MapNodePath + "_outline.png"];
    public override string? CustomRunHistoryIconPath =>
        RunHistoryIconPath;
    public override string? CustomRunHistoryIconOutlinePath =>
        RunHistoryOutlinePath;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<Fremont>()];

    protected override IReadOnlyList<(MonsterModel, string?)>
        GenerateMonsters() =>
        [(ModelDb.Monster<Fremont>().ToMutable(), null)];
}
