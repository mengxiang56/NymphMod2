using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Cards;
using Nymph.Mechanics;
using Nymph.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class FremontMechanicsPower : ModPowerTemplate
{
    private sealed class RuntimeData
    {
        public HashSet<CardModel> PendingCoffins { get; } = [];
        public Dictionary<Creature, int> BlockTowardEvokeByPlayer { get; }
            = [];
        public bool ResolvingSecondPhase { get; set; }
    }

    private static readonly BlockingPlayerChoiceContext ChoiceContext = new();

    private int _cardsTowardOrb;
    private int _coffinsCreatedThisTurn;
    private int _orbCount;
    private bool _secondPhase;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;

    [SavedProperty]
    public int CardsTowardOrb
    {
        get => _cardsTowardOrb;
        set
        {
            AssertMutable();
            _cardsTowardOrb = Math.Clamp(value, 0, 4);
        }
    }

    [SavedProperty]
    public int CoffinsCreatedThisTurn
    {
        get => _coffinsCreatedThisTurn;
        set
        {
            AssertMutable();
            _coffinsCreatedThisTurn = Math.Clamp(value, 0, 2);
        }
    }

    [SavedProperty]
    public bool SecondPhase
    {
        get => _secondPhase;
        set
        {
            AssertMutable();
            _secondPhase = value;
        }
    }

    [SavedProperty]
    public int OrbCount
    {
        get => _orbCount;
        set
        {
            AssertMutable();
            _orbCount = Math.Clamp(value, 0, 3);
        }
    }

    protected override object InitInternalData() => new RuntimeData();

    public override async Task AfterApplied(
        Creature? applier,
        CardModel? cardSource)
    {
        if (OrbCount == 0)
        {
            OrbCount = 1;
        }

        FremontOrbVisuals.Sync(Owner, OrbCount, animateNewOrbs: true);
        await Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
#if !STS2_PUBLIC
        ,
        CardPlay? cardPlay
#endif
        )
    {
        return target == Owner
            ? Math.Max(0m, 1m - OrbCount * 0.25m)
            : 1m;
    }

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            CoffinsCreatedThisTurn = 0;
            RuntimeData data = GetInternalData<RuntimeData>();
            data.PendingCoffins.Clear();
            data.BlockTowardEvokeByPlayer.Clear();
        }

        return Task.CompletedTask;
    }

#if STS2_PUBLIC
    public override (PileType, CardPilePosition)
        ModifyCardPlayResultPileTypeAndPosition(
            CardModel card,
            bool isAutoPlay,
            ResourceInfo resources,
            PileType pileType,
            CardPilePosition position)
    {
        if (!TryMarkForCoffin(card))
        {
            return (pileType, position);
        }

        return (PileType.None, CardPilePosition.Bottom);
    }
#else
    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        if (!TryMarkForCoffin(card))
        {
            return cardLocation;
        }

        return new CardLocation(
            card.Owner,
            PileType.None,
            CardPilePosition.Bottom);
    }
#endif

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (!cardPlay.IsLastInSeries)
        {
            return;
        }

        RuntimeData data = GetInternalData<RuntimeData>();
        if (data.PendingCoffins.Remove(cardPlay.Card))
        {
            await CreateCoffin(cardPlay.Card);
        }

        int progress = CardsTowardOrb + 1;
        if (progress >= 5)
        {
            CardsTowardOrb = 0;
            await GenerateOrb(cardPlay.Card.Owner.Creature);
        }
        else
        {
            CardsTowardOrb = progress;
        }
    }

    public override async Task AfterBlockGained(
        Creature creature,
        decimal amount,
        ValueProp props,
        CardModel? cardSource)
    {
        if (!creature.IsPlayer || creature.Side == Owner.Side || amount <= 0)
        {
            return;
        }

        RuntimeData data = GetInternalData<RuntimeData>();
        int total = data.BlockTowardEvokeByPlayer.GetValueOrDefault(creature)
            + (int)amount;
        int evokeCount = total / 10;
        data.BlockTowardEvokeByPlayer[creature] = total % 10;

        for (int i = 0; i < evokeCount; i++)
        {
            await EvokeOrb(creature);
            if (creature.IsDead)
            {
                break;
            }
        }
    }

    public override decimal ModifyDamageCap(
        Creature? target,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
#if !STS2_PUBLIC
        ,
        CardPlay? cardPlay
#endif
        )
    {
        if (target != Owner || SecondPhase)
        {
            return decimal.MaxValue;
        }

        return Math.Max(0, Owner.CurrentHp - Owner.MaxHp / 2);
    }

    public override async Task AfterCurrentHpChanged(
        Creature creature,
        decimal delta)
    {
        if (creature != Owner
            || delta >= 0
            || SecondPhase
            || Owner.CurrentHp > Owner.MaxHp / 2)
        {
            return;
        }

        await BeginSecondPhase();
    }

    public static bool IsMarkedForCoffin(CardModel card)
    {
        ICombatState? combatState = card.Owner.Creature.CombatState;
        return combatState?.Enemies
            .Select(enemy => enemy.GetPower<FremontMechanicsPower>())
            .Any(power => power is not null
                && power.GetInternalData<RuntimeData>()
                    .PendingCoffins.Contains(card)) == true;
    }

    private bool TryMarkForCoffin(CardModel card)
    {
        if (card is NymphExiledBlackCoffin
            || card.Owner.Creature.Side == Owner.Side
            || CoffinsCreatedThisTurn >= 2)
        {
            return false;
        }

        RuntimeData data = GetInternalData<RuntimeData>();
        if (!data.PendingCoffins.Add(card))
        {
            return true;
        }

        CoffinsCreatedThisTurn++;
        return true;
    }

    private async Task CreateCoffin(CardModel original)
    {
        ICombatState? combatState = original.CombatState
            ?? original.Owner.Creature.CombatState;
        if (combatState is null || CombatManager.Instance.IsEnding)
        {
            return;
        }

        NymphExiledBlackCoffin coffin =
            combatState.CreateCard<NymphExiledBlackCoffin>(original.Owner);
        coffin.OriginalCard = original.ToSerializable();
        coffin.OriginalDeckVersions = original.DeckVersion is null
            ? []
            : [original.DeckVersion.ToSerializable()];
        await CardPileCmd.AddGeneratedCardToCombat(
            coffin,
            PileType.Hand,
            original.Owner);
    }

    private async Task GenerateOrb(Creature evokeTarget)
    {
        if (OrbCount >= 3)
        {
            await EvokeOrb(evokeTarget);
        }

        OrbCount++;
        FremontOrbVisuals.Sync(Owner, OrbCount, animateNewOrbs: true);
    }

    private async Task EvokeOrb(Creature target)
    {
        if (OrbCount <= 0 || Owner.IsDead)
        {
            return;
        }

        OrbCount--;
        FremontOrbVisuals.EvokeOne(Owner, OrbCount);
        VfxCmd.PlayOnCreature(target, "vfx/vfx_attack_lightning");
        await CreatureCmd.Damage(
            ChoiceContext,
            target,
            10,
            DamageProps.nonCardUnpowered,
            Owner);
    }

    private async Task BeginSecondPhase()
    {
        RuntimeData data = GetInternalData<RuntimeData>();
        if (data.ResolvingSecondPhase || SecondPhase)
        {
            return;
        }

        data.ResolvingSecondPhase = true;
        SecondPhase = true;
        try
        {
            if (Owner.Monster is Fremont fremont)
            {
                fremont.EnterSecondPhase();
            }

            foreach (var player in Owner.CombatState!.Players)
            {
                List<NymphExiledBlackCoffin> coffins =
                GetRedistributableCards(player)
                    .OfType<NymphExiledBlackCoffin>()
                    .ToList();
                foreach (NymphExiledBlackCoffin coffin in coffins)
                {
                    await CardCmd.Exhaust(
                        ChoiceContext,
                        coffin,
                        causedByEthereal: false);
                    await CreatureCmd.Heal(Owner, 25);
                }

                await RedistributeCards(player);
            }
        }
        finally
        {
            data.ResolvingSecondPhase = false;
        }
    }

    private static IEnumerable<CardModel> GetRedistributableCards(
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        return PileType.Hand.GetPile(player).Cards
            .Concat(PileType.Draw.GetPile(player).Cards)
            .Concat(PileType.Discard.GetPile(player).Cards)
            .ToList();
    }

    private static async Task RedistributeCards(
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        List<CardModel> cards = GetRedistributableCards(player).ToList();
        player.RunState.Rng.Shuffle.Shuffle(cards);

        List<CardModel> drawCards = [];
        List<CardModel> discardCards = [];
        foreach (CardModel card in cards)
        {
            (player.RunState.Rng.Shuffle.NextBool()
                ? drawCards
                : discardCards).Add(card);
        }

        await CardPileCmd.Add(
            drawCards,
            PileType.Draw,
            CardPilePosition.Bottom);
        await CardPileCmd.Add(
            discardCards,
            PileType.Discard,
            CardPilePosition.Bottom);
    }
}
