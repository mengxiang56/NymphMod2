using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using Nymph.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Encounters;

[RegisterActEncounter(typeof(Glory))]
public sealed class FremontBoss : ModEncounterTemplate
{
    private const string RunHistoryIconPath =
        "res://Nymph/images/characters/Nymph_character_icon.png";
    private const string RunHistoryOutlinePath =
        "res://Nymph/images/characters/Nymph_character_icon_outline.png";

    public override RoomType RoomType => RoomType.Boss;
    public override string CustomBgm => "nymph:/music/fremont";
    public override string? CustomBossNodePath =>
        ModelDb.Encounter<QueenBoss>().BossNodePath;
    public override IEnumerable<string>? CustomMapNodeAssetPaths =>
        [ModelDb.Encounter<QueenBoss>().BossNodePath];
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
