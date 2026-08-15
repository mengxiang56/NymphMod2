using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using Nymph.Encounters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Acts;

[RegisterAct]
public sealed class QuilonAct : ModActTemplate
{
    public override int Index => -1;
    public override bool IsDefault => false;
    public override ActAssetProfile AssetProfile => ContentAssetProfiles.FromVanillaActId("glory");
    public override string[] BgMusicOptions => ["event:/music/act3_a1_v1"];
    public override string[] MusicBankPaths => ["res://banks/desktop/act3_a1.bank"];
    public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";
    public override string ChestSpineSkinNameNormal => "act3";
    public override string ChestSpineSkinNameStroke => "act3_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";
    public override Color MapTraveledColor => new("6F274B");
    public override Color MapUntraveledColor => new("D59AB8");
    public override Color MapBgColor => new("392634");
    protected override int BaseNumberOfRooms => 2;
    public override IEnumerable<EncounterModel> BossDiscoveryOrder => [ModelDb.Encounter<QuilonBossEncounter>()];
    public override IEnumerable<AncientEventModel> AllAncients => [];
    public override IEnumerable<EventModel> AllEvents => [];
    public override IEnumerable<EncounterModel> GenerateAllEncounters() => [ModelDb.Encounter<QuilonBossEncounter>()];
    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState) => [];
    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState) { }
    public override bool IsUnlocked(UnlockState unlockState) => false;
    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) => new(0, 1);
}

public sealed class QuilonActMap : ActMap
{
    protected override MapPoint?[,] Grid { get; }
    public override MapPoint StartingMapPoint { get; }
    public override MapPoint BossMapPoint { get; }

    public QuilonActMap()
    {
        Grid = new MapPoint[7, 2];
        StartingMapPoint = new MapPoint(3, 0) { PointType = MapPointType.RestSite, CanBeModified = false };
        MapPoint shop = new(3, 1) { PointType = MapPointType.Shop, CanBeModified = false };
        BossMapPoint = new MapPoint(3, 2) { PointType = MapPointType.Boss, CanBeModified = false };
        Grid[3, 1] = shop;
        StartingMapPoint.AddChildPoint(shop);
        shop.AddChildPoint(BossMapPoint);
        startMapPoints.Add(shop);
    }
}
