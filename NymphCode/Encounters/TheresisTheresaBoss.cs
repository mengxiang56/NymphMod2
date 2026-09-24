using Godot;
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
[RegisterActEncounter(typeof(Overgrowth))]
public sealed class TheresisTheresaBoss : ModEncounterTemplate
{
    private const string RunHistoryIconPath =
        "res://Nymph/images/characters/Nymph_character_icon.png";
    private const string RunHistoryOutlinePath =
        "res://Nymph/images/characters/Nymph_character_icon_outline.png";

    internal const string ShadowSlot0 = "shadow_0";
    internal const string ShadowSlot1 = "shadow_1";
    internal const string ShadowSlot2 = "shadow_2";
    internal const string ShadowSlot3 = "shadow_3";
    internal const string LordSlot4 = "lord_4";
    internal const string SageSlot = "sage";
    internal const string PhaseTwoSageSlot = "phase_two_sage";

    public override RoomType RoomType => RoomType.Boss;
    public override string CustomBgm => "nymph:/music/theresis_theresa";
    public override string? CustomBossNodePath =>
        ModelDb.Encounter<QueenBoss>().BossNodePath;
    public override IEnumerable<string>? CustomMapNodeAssetPaths =>
        [ModelDb.Encounter<QueenBoss>().BossNodePath];
    public override string? CustomRunHistoryIconPath =>
        RunHistoryIconPath;
    public override string? CustomRunHistoryIconOutlinePath =>
        RunHistoryOutlinePath;

    public override IReadOnlyList<string> Slots =>
    [
        ShadowSlot0,
        ShadowSlot1,
        ShadowSlot2,
        ShadowSlot3,
        LordSlot4,
        SageSlot,
        PhaseTwoSageSlot
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<NymphTheresis>(),
        ModelDb.Monster<NymphTheresa>(),
        ModelDb.Monster<NymphSovereignShadow>()
    ];

    protected override bool SuppliesEncounterCombatSceneFromFactory => true;

    protected override Control TryCreateEncounterCombatScene()
    {
        Control root = new()
        {
            Name = "TheresisTheresaSlots",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        AddSlot(root, ShadowSlot0, new Vector2(760f, 730f));
        AddSlot(root, ShadowSlot1, new Vector2(950f, 730f));
        AddSlot(root, ShadowSlot2, new Vector2(1140f, 730f));
        AddSlot(root, ShadowSlot3, new Vector2(1330f, 730f));
        AddSlot(root, LordSlot4, new Vector2(1550f, 730f));
        AddSlot(root, SageSlot, new Vector2(1720f, 730f));
        AddSlot(root, PhaseTwoSageSlot, new Vector2(1310f, 730f));
        return root;
    }

    protected override IReadOnlyList<(MonsterModel, string?)>
        GenerateMonsters()
    {
        NymphSovereignShadow shadow =
            (NymphSovereignShadow)ModelDb.Monster<NymphSovereignShadow>()
                .ToMutable();
        shadow.SlotIndex = 3;
        shadow.StartsWithSlash = true;
        return
        [
            (ModelDb.Monster<NymphTheresis>().ToMutable(), LordSlot4),
            (ModelDb.Monster<NymphTheresa>().ToMutable(), SageSlot),
            (shadow, ShadowSlot3)
        ];
    }

    internal static string GetShadowSlot(int index) => index switch
    {
        0 => ShadowSlot0,
        1 => ShadowSlot1,
        2 => ShadowSlot2,
        3 => ShadowSlot3,
        4 => LordSlot4,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    private static void AddSlot(Control root, string name, Vector2 position)
    {
        Marker2D marker = new()
        {
            Name = name,
            Position = position
        };
        root.AddChild(marker);
    }
}
