using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Powers;
using Nymph.Rewards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

public abstract class NymphInspirationCard : ModCardTemplate
{
    protected const int CreateAmount = 5;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.InspirationCard,
        NymphKeywords.Conceive
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath:
            $"{Entry.ResPath}/images/cards/inspiration/{GetType().Name}.png",
        FramePath:
            $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected NymphInspirationCard(
        CardRarity rarity,
        TargetType targetType = TargetType.Self)
        : base(0, CardType.Skill, rarity, targetType, true)
    {
    }

    protected sealed override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        InspirationMechanics.ConsumeSelected(this);
        await ThoughtMechanics.Create(
            choiceContext,
            Owner,
            CreateAmount,
            this);
        await ApplyInspiration(choiceContext, cardPlay);
        await InspirationMechanics.RemoveFromCombatAfterPlayed(this);
    }

    protected abstract Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay);
}

public abstract class NymphHandCostInspiration : NymphInspirationCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new DynamicVar("Amount", 1)
    ];

    protected NymphHandCostInspiration(CardRarity rarity)
        : base(rarity)
    {
    }

    protected void ReduceHandCosts(bool forCombat)
    {
        foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards)
        {
            if (card == this)
            {
                continue;
            }

            if (forCombat)
            {
                card.EnergyCost.AddThisCombat(-DynamicVars["Amount"].IntValue);
            }
            else
            {
                card.EnergyCost.AddThisTurn(-DynamicVars["Amount"].IntValue);
            }
        }
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationMercenary : NymphHandCostInspiration
{
    public NymphInspirationMercenary() : base(CardRarity.Uncommon)
    {
    }

    protected override Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ReduceHandCosts(forCombat: false);
        return Task.CompletedTask;
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationCivilWar : NymphHandCostInspiration
{
    public NymphInspirationCivilWar() : base(CardRarity.Rare)
    {
    }

    protected override Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ReduceHandCosts(forCombat: true);
        return Task.CompletedTask;
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationEnlightenment : NymphInspirationCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new EnergyVar(1),
        new DynamicVar("Turns", 2)
    ];

    public NymphInspirationEnlightenment()
        : base(CardRarity.Uncommon)
    {
    }

    protected override async Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await PowerCmd.Apply<RadiancePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Turns"].IntValue,
            Owner.Creature,
            this);
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationGathering : NymphInspirationCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.InspirationId)
    ];

    public NymphInspirationGathering()
        : base(CardRarity.Rare)
    {
    }

    protected override Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (Owner.RunState.CurrentRoom is CombatRoom combatRoom)
        {
            combatRoom.AddExtraReward(
                Owner,
                new CardReward(
                    CardCreationOptions.ForRoom(Owner, combatRoom.RoomType),
                    3,
                    Owner));
            combatRoom.AddExtraReward(Owner, new InspirationReward(Owner));
        }

        return Task.CompletedTask;
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationTorment : NymphInspirationCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new PowerVar<WeakPower>(1),
        new PowerVar<VulnerablePower>(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public NymphInspirationTorment()
        : base(CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        var enemies = CombatState!
            .GetOpponentsOf(Owner.Creature)
            .Where(enemy => !enemy.IsDead)
            .ToList();

        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            enemies,
            DynamicVars.Weak.IntValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            enemies.Where(enemy => !enemy.IsDead),
            DynamicVars.Vulnerable.IntValue,
            Owner.Creature,
            this);
    }
}

public abstract class NymphSelfPowerInspiration<TPower>
    : NymphInspirationCard
    where TPower : MegaCrit.Sts2.Core.Models.PowerModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new DynamicVar("Amount", Amount)
    ];

    protected abstract int Amount { get; }

    protected NymphSelfPowerInspiration(CardRarity rarity)
        : base(rarity)
    {
    }

    protected virtual bool IncludeSelfPowerHoverTip => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        IncludeSelfPowerHoverTip
            ? [HoverTipFactory.FromPower<TPower>()]
            : [];

    protected override async Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<TPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Amount"].IntValue,
            Owner.Creature,
            this);
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationWall
    : NymphSelfPowerInspiration<BufferPower>
{
    protected override int Amount => 1;
    public NymphInspirationWall() : base(CardRarity.Uncommon)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationRelocation
    : NymphSelfPowerInspiration<IntangiblePower>
{
    protected override int Amount => 1;
    public NymphInspirationRelocation() : base(CardRarity.Rare)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationMarch
    : NymphSelfPowerInspiration<StrengthPower>
{
    protected override int Amount => 4;
    public NymphInspirationMarch() : base(CardRarity.Rare)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationOathbreak
    : NymphSelfPowerInspiration<ThornsPower>
{
    protected override int Amount => 3;
    public NymphInspirationOathbreak() : base(CardRarity.Common)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationRest
    : NymphSelfPowerInspiration<RegenPower>
{
    protected override int Amount => 3;
    public NymphInspirationRest() : base(CardRarity.Common)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationSleep
    : NymphSelfPowerInspiration<RegenPower>
{
    protected override int Amount => 4;
    public NymphInspirationSleep() : base(CardRarity.Uncommon)
    {
    }
}

public abstract class NymphStrengthTransferInspiration
    : NymphInspirationCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new DynamicVar("Amount", Amount)
    ];

    protected abstract int Amount { get; }

    protected NymphStrengthTransferInspiration(CardRarity rarity)
        : base(rarity, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override async Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Amount"].IntValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            cardPlay.Target,
            -DynamicVars["Amount"].IntValue,
            Owner.Creature,
            this);
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationPillage
    : NymphStrengthTransferInspiration
{
    protected override int Amount => 1;
    public NymphInspirationPillage() : base(CardRarity.Common)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationInvasion
    : NymphStrengthTransferInspiration
{
    protected override int Amount => 2;
    public NymphInspirationInvasion() : base(CardRarity.Uncommon)
    {
    }
}

public abstract class NymphGoldInspiration : NymphInspirationCard
{
    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new DynamicVar("Gold", Gold)
    ];

    protected abstract int Gold { get; }

    protected NymphGoldInspiration(CardRarity rarity) : base(rarity)
    {
    }

    protected override async Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, Owner);
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationCatastrophe
    : NymphGoldInspiration
{
    protected override int Gold => 20;
    public NymphInspirationCatastrophe() : base(CardRarity.Uncommon)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationFurnace : NymphGoldInspiration
{
    protected override int Gold => 50;
    public NymphInspirationFurnace() : base(CardRarity.Rare)
    {
    }
}

public abstract class NymphDrawInspiration<TPower>
    : NymphInspirationCard
    where TPower : MegaCrit.Sts2.Core.Models.PowerModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new CardsVar(DrawAmount),
        new DynamicVar("Turns", 2)
    ];

    protected abstract int DrawAmount { get; }

    protected NymphDrawInspiration(CardRarity rarity) : base(rarity)
    {
    }

    protected override async Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CardPileCmd.Draw(
            choiceContext,
            DynamicVars.Cards.IntValue,
            Owner);
        await PowerCmd.Apply<TPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Turns"].IntValue,
            Owner.Creature,
            this);
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationOutblood
    : NymphDrawInspiration<InspirationOutbloodPower>
{
    protected override int DrawAmount => 1;
    public NymphInspirationOutblood() : base(CardRarity.Common)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationFlames
    : NymphDrawInspiration<InspirationFlamesPower>
{
    protected override int DrawAmount => 2;
    public NymphInspirationFlames() : base(CardRarity.Uncommon)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationLost : NymphInspirationCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.InspirationId)
    ];

    public NymphInspirationLost() : base(CardRarity.Common)
    {
    }

    protected override Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (Owner.RunState.CurrentRoom is CombatRoom combatRoom)
        {
            combatRoom.AddExtraReward(
                Owner,
                new InspirationReward(Owner));
        }

        return Task.CompletedTask;
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationCityDriving
    : NymphSelfPowerInspiration<InspirationCityDrivingPower>
{
    protected override int Amount => 1;
    protected override bool IncludeSelfPowerHoverTip => false;

    public NymphInspirationCityDriving()
        : base(CardRarity.Uncommon)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationTemperBlade
    : NymphSelfPowerInspiration<InspirationTemperBladePower>
{
    protected override int Amount => 1;
    protected override bool IncludeSelfPowerHoverTip => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>()
    ];

    public NymphInspirationTemperBlade()
        : base(CardRarity.Uncommon)
    {
    }
}

public abstract class NymphBlockInspiration : NymphInspirationCard
{
    public override bool GainsBlock => true;

    protected abstract decimal BlockAmount { get; }

    protected NymphBlockInspiration(CardRarity rarity) : base(rarity)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new BlockVar(BlockAmount, ValueProp.Move)
    ];

    protected override async Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block,
            cardPlay);
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationConsume : NymphBlockInspiration
{
    protected override decimal BlockAmount => 8;

    public NymphInspirationConsume() : base(CardRarity.Common)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationReclaim : NymphBlockInspiration
{
    protected override decimal BlockAmount => 12;

    public NymphInspirationReclaim() : base(CardRarity.Uncommon)
    {
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationRiseAndFall : NymphInspirationCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new BlockVar(12, ValueProp.Move),
        new DynamicVar("Amount", 2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public NymphInspirationRiseAndFall() : base(CardRarity.Rare)
    {
    }

    protected override async Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block,
            cardPlay);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Amount"].IntValue,
            Owner.Creature,
            this);
    }
}

[RegisterCard(typeof(NymphInspirationCardPool))]
public sealed class NymphInspirationDeadFight : NymphInspirationCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", CreateAmount),
        new CardsVar(3)
    ];

    public NymphInspirationDeadFight()
        : base(CardRarity.Uncommon)
    {
    }

    protected override Task ApplyInspiration(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        List<CardModel> candidates = PileType.Hand
            .GetPile(Owner)
            .Cards
            .Where(card => card is not NymphInspirationCard
                && card.Type is not CardType.Status
                && card.Type is not CardType.Curse)
            .ToList();

        int count = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            CardModel? selected = Owner.RunState.Rng
                .CombatCardSelection
                .NextItem(candidates);
            if (selected is null)
            {
                break;
            }

            candidates.Remove(selected);
            DoubleCardNumbers(selected);
        }

        return Task.CompletedTask;
    }

    private static void DoubleCardNumbers(CardModel card)
    {
        foreach (DynamicVar variable in card.DynamicVars.Values)
        {
            if (variable.Name == "Energy" || variable.BaseValue < 0)
            {
                continue;
            }

            variable.BaseValue *= 2;
            variable.ResetToBase();
        }

        NCard? node = NCard.FindOnTable(card);
        node?.UpdateVisuals(
            card.Pile?.Type ?? PileType.Hand,
            CardPreviewMode.Normal);
    }
}
