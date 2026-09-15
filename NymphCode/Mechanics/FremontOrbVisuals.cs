using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Nymph.Powers;

namespace Nymph.Mechanics;

internal static class FremontOrbVisuals
{
    internal const string ContainerName = "FremontOrbContainer";
    private const int SlotCount = 3;
    private const float LayoutAngle = 125f;
    private const float LayoutRadius = 225f;
    private const float MiddleSlotLift = 80f;

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
            UpdateValueLabels(current, isEvoking: false);
        }
    }

    internal static void EvokeOne(Creature owner, int remainingCount)
    {
        Control? container = GetOrCreateContainer(owner);
        if (container is null)
        {
            return;
        }

        List<NOrb> slots = container.GetChildren()
            .OfType<NOrb>()
            .OrderBy(GetSlotIndex)
            .Where(slot => GetSlotIndex(slot) < SlotCount)
            .ToList();
        NOrb? firstFilled = slots.FirstOrDefault(slot => slot.Model is not null);

        if (firstFilled is null)
        {
            Sync(owner, remainingCount, animateNewOrbs: false);
            return;
        }

        firstFilled.Name = $"FremontOrbEvoking{firstFilled.GetInstanceId()}";
        firstFilled.UpdateVisuals(isEvoking: true);
        UpdateValueLabels(firstFilled, isEvoking: true);
        ModelDb.Orb<LightningOrb>().PlayEvokeSfx();

        // Remove the evoked orb from the logical slot container immediately.
        // Keeping it there during the fade allowed later slot synchronization to
        // observe the stale visual and made the orb appear not to disappear.
        Control? vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
        if (vfxContainer is not null)
        {
            firstFilled.Reparent(vfxContainer, keepGlobalTransform: true);
        }

        firstFilled.MouseFilter = Control.MouseFilterEnum.Ignore;
        firstFilled.FocusMode = Control.FocusModeEnum.None;
        Tween fadeTween = firstFilled.CreateTween();
        fadeTween.TweenProperty(firstFilled, "modulate:a", 0f, 0.25f);
        fadeTween.Chain().TweenCallback(
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(firstFilled))
                {
                    firstFilled.Visible = false;
                    firstFilled.QueueFreeSafely();
                }
            }));

        List<NOrb> remainingSlots = slots
            .Where(slot => slot != firstFilled)
            .ToList();
        for (int i = 0; i < remainingSlots.Count; i++)
        {
            remainingSlots[i].Name = GetSlotName(i);
            UpdateValueLabels(remainingSlots[i], isEvoking: false);
        }

        NOrb emptySlot = NOrb.Create(isLocal: true);
        emptySlot.Name = GetSlotName(SlotCount - 1);
        container.AddChildSafely(emptySlot);
        emptySlot.Position = Vector2.Zero;
        UpdateValueLabels(emptySlot, isEvoking: false);
        remainingSlots.Add(emptySlot);

        Tween layoutTween = container.CreateTween().SetParallel();
        for (int i = 0; i < remainingSlots.Count; i++)
        {
            layoutTween.TweenProperty(
                    remainingSlots[i],
                    "position",
                    GetSlotPosition(i),
                    0.45f)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
        }
    }

    internal static bool IsFremontOrb(NOrb orb) =>
        orb.GetParent()?.Name == ContainerName;

    internal static void UpdateValueLabels(NOrb orb, bool isEvoking)
    {
        if (!IsFremontOrb(orb) || !orb.IsNodeReady())
        {
            return;
        }

        bool filled = orb.Model is not null;
        orb._labelContainer.Visible = filled;
        orb._passiveLabel.Visible = false;
        orb._evokeLabel.Visible = filled;
        if (filled)
        {
            orb._evokeLabel.SetTextAutoSize(
                FremontMechanicsPower.OrbEvokeDamage.ToString());
        }
    }

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
        if (oldSlot is not null)
        {
            // Free the slot name before adding its replacement. Godot otherwise
            // uniquifies the new node's name, so later sync/evoke calls cannot
            // find the filled slot by index.
            container.RemoveChildSafely(oldSlot);
            oldSlot.QueueFreeSafely();
        }

        NOrb newSlot = NOrb.Create(
            isLocal: true,
            filled ? CreateLightningModel(owner) : null);
        newSlot.Name = GetSlotName(index);
        container.AddChildSafely(newSlot);
        newSlot.Position = GetSlotPosition(index);

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
        Vector2 position = new Vector2(
            -Mathf.Cos(radians),
            Mathf.Sin(radians)) * LayoutRadius;
        return index == SlotCount / 2
            ? position + Vector2.Up * MiddleSlotLift
            : position;
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
