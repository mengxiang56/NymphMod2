using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphHopeEraGraffiti : ModRelicTemplate
{
    private int _remainingCombats = 3;
    private bool _activeThisCombat;
    private bool _usedUp;

    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool IsUsedUp => UsedUp;
    public override bool ShowCounter => DisplayAmount > 0;
    public override int DisplayAmount => RemainingCombats;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/NymphHopeEraGraffiti.png",
        IconOutlinePath:
            $"{Entry.ResPath}/images/relics/NymphHopeEraGraffiti_outline.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/NymphHopeEraGraffiti.png");

    [SavedProperty]
    public int RemainingCombats
    {
        get => _remainingCombats;
        set
        {
            AssertMutable();
            _remainingCombats = value;
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public bool ActiveThisCombat
    {
        get => _activeThisCombat;
        set
        {
            AssertMutable();
            _activeThisCombat = value;
        }
    }

    [SavedProperty]
    public bool UsedUp
    {
        get => _usedUp;
        set
        {
            AssertMutable();
            _usedUp = value;
            Status = value ? RelicStatus.Disabled : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeCombatStart()
    {
        ActiveThisCombat = RemainingCombats > 0;
        if (ActiveThisCombat)
        {
            RemainingCombats--;
            Flash();
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ActiveThisCombat = false;
        if (RemainingCombats <= 0)
        {
            UsedUp = true;
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyDamageCap(
        Creature? target,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
#if !STS2_PUBLIC
        ,
        CardPlay? cardPlay)
#else
        )
#endif
    {
        return ActiveThisCombat
            && Owner.PlayerCombatState?.TurnNumber == 1
            && dealer?.CombatState == Owner.Creature.CombatState
            ? 0
            : decimal.MaxValue;
    }
}

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphNemesisEraHatred : ModRelicTemplate
{
    private int _remainingCombats = 2;
    private bool _activeThisCombat;
    private bool _usedUp;

    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool IsUsedUp => UsedUp;
    public override bool ShowCounter => DisplayAmount > 0;
    public override int DisplayAmount => RemainingCombats;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/NymphNemesisEraHatred.png",
        IconOutlinePath:
            $"{Entry.ResPath}/images/relics/NymphNemesisEraHatred_outline.png",
        BigIconPath:
            $"{Entry.ResPath}/images/relics/NymphNemesisEraHatred.png");

    [SavedProperty]
    public int RemainingCombats
    {
        get => _remainingCombats;
        set
        {
            AssertMutable();
            _remainingCombats = value;
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public bool ActiveThisCombat
    {
        get => _activeThisCombat;
        set
        {
            AssertMutable();
            _activeThisCombat = value;
        }
    }

    [SavedProperty]
    public bool UsedUp
    {
        get => _usedUp;
        set
        {
            AssertMutable();
            _usedUp = value;
            Status = value ? RelicStatus.Disabled : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task BeforeCombatStart()
    {
        ActiveThisCombat = RemainingCombats > 0;
        if (!ActiveThisCombat)
        {
            return;
        }

        RemainingCombats--;
        Flash();
        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        PlayerChoiceContext choiceContext = new ThrowingPlayerChoiceContext();
        foreach (Creature enemy in combatState.GetOpponentsOf(Owner.Creature))
        {
            if (!enemy.IsDead)
            {
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext,
                    enemy,
                    3,
                    Owner.Creature,
                    null);
            }
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (ActiveThisCombat)
        {
            room.AddExtraReward(Owner, new RelicReward(Owner));
            ActiveThisCombat = false;
            Flash();
        }

        if (RemainingCombats <= 0)
        {
            UsedUp = true;
        }

        return Task.CompletedTask;
    }
}

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphBeautifulWishEraRemembrance : ModRelicTemplate
{
    private const int TriggerCount = 2;
    private int _remainingTriggers;
    private bool _grantingBonus;
    private bool _usedUp;

    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool IsUsedUp => UsedUp;
    public override bool ShowCounter => DisplayAmount > 0;
    public override int DisplayAmount => RemainingTriggers;

    public override RelicAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/relics/NymphBeautifulWishEraRemembrance.png",
        IconOutlinePath:
            $"{Entry.ResPath}/images/relics/NymphBeautifulWishEraRemembrance_outline.png",
        BigIconPath:
            $"{Entry.ResPath}/images/relics/NymphBeautifulWishEraRemembrance.png");

    [SavedProperty]
    public int RemainingTriggers
    {
        get => _remainingTriggers;
        set
        {
            AssertMutable();
            _remainingTriggers = value;
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public bool UsedUp
    {
        get => _usedUp;
        set
        {
            AssertMutable();
            _usedUp = value;
            Status = value ? RelicStatus.Disabled : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    internal bool IsGrantingBonus => _grantingBonus;

    internal void Activate()
    {
        UsedUp = false;
        RemainingTriggers = TriggerCount;
    }

    internal bool TryBeginBonus()
    {
        if (_grantingBonus || RemainingTriggers <= 0)
        {
            return false;
        }

        RemainingTriggers--;
        _grantingBonus = true;
        Flash();
        return true;
    }

    internal void EndBonus()
    {
        _grantingBonus = false;
        if (RemainingTriggers <= 0)
        {
            UsedUp = true;
        }
    }
}
