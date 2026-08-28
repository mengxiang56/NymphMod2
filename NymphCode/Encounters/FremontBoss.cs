using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using Nymph.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Encounters;

[RegisterActEncounter(typeof(Hive))]
public sealed class FremontBoss : ModEncounterTemplate
{
    public override RoomType RoomType => RoomType.Boss;
    public override string CustomBgm => "event:/music/act3_boss_queen";
    public override string BossNodePath =>
        ModelDb.Encounter<QueenBoss>().BossNodePath;
    public override MegaSkeletonDataResource? BossNodeSpineResource =>
        ModelDb.Encounter<QueenBoss>().BossNodeSpineResource;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<Fremont>()];

    protected override IReadOnlyList<(MonsterModel, string?)>
        GenerateMonsters() =>
        [(ModelDb.Monster<Fremont>().ToMutable(), null)];
}
