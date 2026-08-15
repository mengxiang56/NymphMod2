using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Nymph.Acts;
using Nymph.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Nymph.Encounters;

[RegisterActEncounter(typeof(QuilonAct))]
public sealed class QuilonBossEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    public override IReadOnlyList<string> Slots => ["envies", "furies", "quilon", "chalice"];
    public override bool HasScene => true;
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Quilon>(), ModelDb.Monster<Envies>(),
        ModelDb.Monster<Furies>(), ModelDb.Monster<ChaliceOfRegret>()
    ];
    public override string BossNodePath => $"{Entry.ResPath}/images/map/quilon_boss_icon";
    public override MegaSkeletonDataResource? BossNodeSpineResource => null;
    public override string CustomBgm => "event:/music/act3_boss_queen";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        [(ModelDb.Monster<Quilon>().ToMutable(), "quilon")];

    public override float GetCameraScaling() => 0.9f;
    public override Vector2 GetCameraOffset() => Vector2.Down * 45f;
}
