using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Nymph.Mechanics;

internal static class FremontOrbVisuals
{
    internal const string ContainerName = "FremontOrbContainer";
    private const int SlotCount = 3;
    private const float LayoutAngle = 125f;
    private const float LayoutRadius = 225f;

    internal static void Sync(
        Creature owner,
        int orbCount,
        bool animateNewOrbs)
    {
        Control? container = GetOrCreateContainer(owner);
        if (container is null)
        {
            return;
        }

        int filledSlots = Math.Clamp(orbCount, 0, SlotCount);
        List<NOrb> slots = container.GetChildren()
            .OfType<NOrb>()
            .OrderBy(GetSlotIndex)
            .ToList();

        for (int i = 0; i < SlotCount; i++)
        {
            bool shouldBeFilled = i < filledSlots;
            NOrb? current = slots.FirstOrDefault(slot =>
                GetSlotIndex(slot) == i);
            if (current is null || (current.Model is not null) != shouldBeFilled)
            {
                current = ReplaceSlot(
                    container,
                    current,
                    owner,
                    i,
                    shouldBeFilled,
                    animateNewOrbs && shouldBeFilled);
            }

            current.Position = GetSlotPosition(i);
            current._labelContainer.Visible = false;
        }
    }

    internal static void EvokeOne(Creature owner, int remainingCount)
    {
        Control? container = GetOrCreateContainer(owner);
        NOrb? firstFilled = container?.GetChildren()
            .OfType<NOrb>()
            .Where(slot => slot.Model is not null)
            .OrderBy(GetSlotIndex)
            .FirstOrDefault();

        if (firstFilled is not null)
        {
            firstFilled.UpdateVisuals(isEvoking: true);
            ModelDb.Orb<LightningOrb>().PlayEvokeSfx();
            container!.RemoveChildSafely(firstFilled);
            firstFilled.QueueFreeSafely();
        }

        Sync(owner, remainingCount, animateNewOrbs: false);
    }

    internal static bool IsFremontOrb(NOrb orb) =>
        orb.GetParent()?.Name == ContainerName;

    private static Control? GetOrCreateContainer(Creature owner)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (creatureNode is null)
        {
            return null;
        }

        Control? existing = creatureNode.GetNodeOrNull<Control>(ContainerName);
        if (existing is not null)
        {
            return existing;
        }

        Control container = new()
        {
            Name = ContainerName,
            Position = creatureNode.Visuals.OrbPosition.Position,
            ZIndex = 20,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        creatureNode.AddChildSafely(container);
        return container;
    }

    private static NOrb ReplaceSlot(
        Control container,
        NOrb? oldSlot,
        Creature owner,
        int index,
        bool filled,
        bool animate)
    {
        NOrb newSlot = NOrb.Create(
            isLocal: true,
            filled ? CreateLightningModel(owner) : null);
        newSlot.Name = GetSlotName(index);
        container.AddChildSafely(newSlot);
        newSlot.Position = GetSlotPosition(index);

        if (oldSlot is not null)
        {
            container.RemoveChildSafely(oldSlot);
            oldSlot.QueueFreeSafely();
        }

        if (animate && filled)
        {
            ModelDb.Orb<LightningOrb>().PlayChannelSfx();
        }

        return newSlot;
    }

    private static LightningOrb CreateLightningModel(Creature owner)
    {
        LightningOrb model =
            (LightningOrb)ModelDb.Orb<LightningOrb>().ToMutable();
        model.Owner = owner.CombatState?.Players.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Fremont orb visuals require a player owner for the base orb model.");
        return model;
    }

    private static Vector2 GetSlotPosition(int index)
    {
        float angle = index * LayoutAngle / (SlotCount - 1);
        float radians = float.DegreesToRadians(-25f - angle);
        return new Vector2(
            -Mathf.Cos(radians),
            Mathf.Sin(radians)) * LayoutRadius;
    }

    private static int GetSlotIndex(NOrb slot)
    {
        string name = slot.Name.ToString();
        return name.StartsWith("FremontOrbSlot", StringComparison.Ordinal)
            && int.TryParse(name["FremontOrbSlot".Length..], out int index)
                ? index
                : int.MaxValue;
    }

    private static string GetSlotName(int index) => $"FremontOrbSlot{index}";
}
