using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Nymph.Monsters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Encounters;

[RegisterActEncounter(typeof(Glory))]
[RegisterActEncounter(typeof(Underdocks))]
public sealed class BodrakastiBoss : ModEncounterTemplate
{
    private const string BossSlot = "bodrakasti";
    private const string FarSlot = "automaton_far";
    private const string MiddleSlot = "automaton_middle";
    private const string NearSlot = "automaton_near";
    private const string RunHistoryIconPath =
        "res://Nymph/images/characters/Nymph_character_icon.png";
    private const string RunHistoryOutlinePath =
        "res://Nymph/images/characters/Nymph_character_icon_outline.png";

    public override RoomType RoomType => RoomType.Boss;
    public override string CustomBgm => "nymph:/music/bodrakasti";
    public override string? CustomBossNodePath =>
        ModelDb.Encounter<QueenBoss>().BossNodePath;
    public override IEnumerable<string>? CustomMapNodeAssetPaths =>
        [ModelDb.Encounter<QueenBoss>().BossNodePath];
    public override string? CustomRunHistoryIconPath => RunHistoryIconPath;
    public override string? CustomRunHistoryIconOutlinePath =>
        RunHistoryOutlinePath;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [
            ModelDb.Monster<Bodrakasti>(),
            ModelDb.Monster<SanctifierAutomaton>()
        ];

    public override IReadOnlyList<string> Slots =>
        [BossSlot, FarSlot, MiddleSlot, NearSlot];

    protected override bool SuppliesEncounterCombatSceneFromFactory => true;

    protected override Control TryCreateEncounterCombatScene()
    {
        Control root = new()
        {
            Name = "BodrakastiSlots",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        AddSlot(root, BossSlot, new Vector2(1440f, 730f));
        AddSlot(root, FarSlot, new Vector2(240f, 730f));
        AddSlot(root, MiddleSlot, new Vector2(480f, 730f));
        AddSlot(root, NearSlot, new Vector2(720f, 730f));
        return root;
    }

    protected override IReadOnlyList<(MonsterModel, string?)>
        GenerateMonsters() =>
        [(ModelDb.Monster<Bodrakasti>().ToMutable(), BossSlot)];

    internal static string GetAutomatonSlot(int distance) =>
        distance switch
        {
            1 => FarSlot,
            2 => MiddleSlot,
            3 => NearSlot,
            _ => throw new ArgumentOutOfRangeException(nameof(distance))
        };

    internal static void AlignAutomatonSlots(ICombatState combatState)
    {
        var player = combatState.Players.FirstOrDefault(
            candidate => !candidate.Creature.IsDead);
        NCombatRoom? room = NCombatRoom.Instance;
        if (player is null
            || room?.EncounterSlots is not { } slots
            || room.GetCreatureNode(player.Creature) is not { } playerNode)
        {
            return;
        }

        int currentDistance = player.Creature
            .GetPower<BodrakastiDistancePower>()?.Amount ?? 2;
        float middleX = playerNode.GlobalPosition.X
            - (currentDistance - 2) * BodrakastiDistancePower.DistanceStep;
        float automatonY = playerNode.GlobalPosition.Y;

        for (int distance = 1; distance <= 3; distance++)
        {
            string slotName = GetAutomatonSlot(distance);
            Marker2D? marker = slots.GetNodeOrNull<Marker2D>(slotName);
            if (marker is null)
            {
                continue;
            }

            marker.GlobalPosition = new Vector2(
                middleX + (distance - 2) * BodrakastiDistancePower.DistanceStep,
                automatonY);

            foreach (var automaton in combatState.Enemies.Where(
                enemy => !enemy.IsDead
                    && enemy.Monster is SanctifierAutomaton model
                    && model.Distance == distance))
            {
                if (room.GetCreatureNode(automaton) is { } automatonNode)
                {
                    automatonNode.GlobalPosition = marker.GlobalPosition;
                }
            }
        }

        UpdateAutomatonFacings(combatState);
    }

    internal static void UpdateAutomatonFacings(ICombatState combatState)
    {
        NCombatRoom? room = NCombatRoom.Instance;
        if (room is null)
        {
            return;
        }

        var playerNodes = combatState.Players
            .Where(player => !player.Creature.IsDead)
            .Select(player => room.GetCreatureNode(player.Creature))
            .Where(node => node is not null)
            .ToList();
        if (playerNodes.Count == 0)
        {
            return;
        }

        foreach (var automaton in combatState.Enemies.Where(
            enemy => !enemy.IsDead
                && enemy.Monster is SanctifierAutomaton))
        {
            if (room.GetCreatureNode(automaton) is not { } automatonNode)
            {
                continue;
            }

            float automatonX = automatonNode.GlobalPosition.X;
            var nearestPlayer = playerNodes.MinBy(
                node => Math.Abs(node!.GlobalPosition.X - automatonX));
            if (nearestPlayer is null)
            {
                continue;
            }

            float difference = nearestPlayer.GlobalPosition.X - automatonX;
            if (Math.Abs(difference) <= 1f)
            {
                continue;
            }

            Node2D body = automatonNode.Visuals.GetNode<Node2D>("%Visuals");
            float scaleX = Math.Abs(body.Scale.X);
            body.Scale = new Vector2(
                difference < 0f ? -scaleX : scaleX,
                body.Scale.Y);
        }
    }

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
