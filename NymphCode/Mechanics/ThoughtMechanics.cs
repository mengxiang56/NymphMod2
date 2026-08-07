using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Runtime.CompilerServices;
using STS2RitsuLib.Cards;
using STS2RitsuLib.Combat.SecondaryResources;
using Nymph.Characters;
using Nymph.Powers;

namespace Nymph.Mechanics;

public enum ThoughtState
{
    Clear,
    Confused,
    Obstructed
}

public static class ThoughtMechanics
{
    public const string LocalId = "Thought";
    public const int ConfusedThreshold = 12;
    public const int ObstructedThreshold = 24;
    public const int MaxAmount = 999;

    /// <summary>
    /// 思绪计数器上行（思绪值）字号。
    /// </summary>
    public const int AmountFontSize = 30;

    /// <summary>
    /// 思绪计数器下行（临界点/阻滞点）字号。
    /// </summary>
    public const int ThresholdFontSize = 22;

    /// <summary>
    /// 思绪值标签相对默认位置的纵向偏移（负值上移）。
    /// </summary>
    public const float AmountLabelVerticalOffset = -10f;

    /// <summary>
    /// 临界点/阻滞点标签相对思绪值标签的纵向偏移（正值下移）。
    /// </summary>
    public const float ThresholdLabelOffsetY = 28f;

    /// <summary>
    /// 思绪计数器数字描边宽度（上下两行共用）。
    /// </summary>
    public const int AmountOutlineSize = 15;

    /// <summary>
    /// 思绪计数器悬停提示框屏幕偏移（正值右移、负值上移；不影响计数器本体）。
    /// </summary>
    public static readonly Vector2 HoverTipScreenOffset = new(48f, -120f);

    private static readonly Vector2 CounterIconSize = new(150f, 110f);

    private const string ClearIconPath =
        $"{Entry.ResPath}/images/ui/thought/thought_clear.png";
    private const string ConfusedIconPath =
        $"{Entry.ResPath}/images/ui/thought/thought_confused.png";
    private const string ObstructedIconPath =
        $"{Entry.ResPath}/images/ui/thought/thought_obstructed.png";

    private static bool _initialized;
    private static readonly ConditionalWeakTable<
        NThoughtCounter,
        CounterVisualState> CounterVisualStates = new();
    private static readonly ConditionalWeakTable<
        CardPlay,
        NarrationRecord> Narrations = new();
    private static readonly ConditionalWeakTable<
        CardPlay,
        CardPlayThoughtStateRecord> CardPlayThoughtStates = new();
    private static readonly ConditionalWeakTable<
        Player,
        PreviousCardRecord> PreviousCards = new();

    private static readonly SecondaryResourceCounterStyle CounterStyle = new()
    {
        CounterSize = CounterIconSize,
        IconSize = CounterIconSize,
        FontSize = AmountFontSize,
        OutlineSize = AmountOutlineSize,
        AmountLabelOffset = new Vector2(0f, 6f),
        ZeroColor = SecondaryResourceCounterStyle.Default.PositiveColor,
        FormatAmount = (amount, _) => amount.ToString(),
        IconStyle = SecondaryResourceIconStyle.Default with
        {
            // 显式设置 IconStyle 时必须带上 Size，否则不会套用 IconSize。
            Size = CounterIconSize,
            HoverTip = SecondaryResourceHoverTipStyle.Default with
            {
                ScreenOffset = HoverTipScreenOffset,
            },
        },
    };

    public static string FormatThresholdLine(int threshold) =>
        $"{threshold}/{threshold * 2}";

    public static string ResourceId =>
        ModSecondaryResourceRegistry.GetResourceId(Entry.ModId, LocalId);

    public static SecondaryResourceDefinition Definition { get; private set; } = null!;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        ModSecondaryResourceRegistry registry =
            ModSecondaryResourceRegistry.For(Entry.ModId);

        Definition = registry.Register(
            LocalId,
            new SecondaryResourceDefinition(
                defaultAmount: 0,
                minAmount: 0,
                hardMaxAmount: MaxAmount,
                persistencePolicy: SecondaryResourcePersistencePolicy.None,
                locTable: "static_hover_tips",
                titleKey: "NYMPH_SECONDARY_RESOURCE_THOUGHT.title",
                descriptionKey: "NYMPH_SECONDARY_RESOURCE_THOUGHT.description",
                smallIconPath: ClearIconPath,
                largeIconPath: ClearIconPath));

        registry.AlwaysShowInCombatUiForCharacter<NymphCharacter>(LocalId);
        registry.RegisterCombatUi<NThoughtCounter>(
            "ThoughtCounter",
            _ => CreateCounter(),
            context => UpdateCounter(context.Node, context.Player));

        CardOnPlayHook.RegisterGlobalListener(new ThoughtCardPlayListener());
    }

    public static int Get(Player player)
    {
        return player.Creature.GetPower<ThoughtPower>()?.Amount ?? 0;
    }

    public static ThoughtState GetState(Player player)
    {
        int amount = Get(player);
        if (amount >= GetObstructedThreshold(player))
        {
            return ThoughtState.Obstructed;
        }

        return amount >= GetConfusedThreshold(player)
            ? ThoughtState.Confused
            : ThoughtState.Clear;
    }

    public static ThoughtState GetState(
        Player player,
        CardPlay? cardPlay)
    {
        return cardPlay is not null
            && CardPlayThoughtStates.TryGetValue(
                cardPlay,
                out CardPlayThoughtStateRecord? record)
            ? record.State
            : GetState(player);
    }

    public static int GetConfusedThreshold(Player player)
    {
        return ConfusedThreshold
            + (player.Creature
                .GetPower<ThoughtThresholdPower>()?.Amount ?? 0);
    }

    public static int GetObstructedThreshold(Player player)
    {
        return GetConfusedThreshold(player) * 2;
    }

    public static async Task IncreaseConfusedThreshold(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount,
        CardModel? source = null)
    {
        if (amount <= 0)
        {
            return;
        }

        await PowerCmd.Apply<ThoughtThresholdPower>(
            choiceContext,
            player.Creature,
            amount,
            player.Creature,
            source,
            silent: true);

        await SyncStatePower(choiceContext, player);
    }

    public static async Task DecreaseConfusedThreshold(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount,
        CardModel? source = null)
    {
        int reduction = Math.Min(
            Math.Max(0, amount),
            Math.Max(0, GetConfusedThreshold(player) - 1));
        if (reduction <= 0)
        {
            return;
        }

        await PowerCmd.Apply<ThoughtThresholdPower>(
            choiceContext,
            player.Creature,
            -reduction,
            player.Creature,
            source,
            silent: true);

        await SyncStatePower(choiceContext, player);
    }

    public static bool CanNarrate(Player? player, int amount)
    {
        return player is not null
            && amount > 0
            && Get(player) >= amount;
    }

    public static bool CanNarrateAll(Player? player)
    {
        return player is not null && Get(player) > 0;
    }

    public static async Task<int> Clear(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? source = null)
    {
        int amount = Get(player);
        if (amount <= 0
            || player.Creature.GetPower<ThoughtPower>() is not { } thought)
        {
            return 0;
        }

        await PowerCmd.ModifyAmount(
            choiceContext,
            thought,
            -amount,
            player.Creature,
            source,
            silent: true);
        await SyncStatePower(choiceContext, player);
        return amount;
    }

    public static async Task Create(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount,
        CardModel? source = null)
    {
        if (player.Creature.HasPower<NoThoughtGainPower>())
        {
            return;
        }

        int availableCapacity = MaxAmount - Get(player);
        int gain = Math.Min(amount, availableCapacity);
        if (gain <= 0)
        {
            return;
        }

        await PowerCmd.Apply<ThoughtPower>(
            choiceContext,
            player.Creature,
            gain,
            player.Creature,
            source,
            silent: true);

        await SyncStatePower(choiceContext, player);

        await PowerCmd.Apply<ConceivedThoughtThisTurnPower>(
            choiceContext,
            player.Creature,
            gain,
            player.Creature,
            source,
            silent: true);

        if (source is not null
            && source.Keywords.Contains(NymphKeywords.Conceive))
        {
            if (player.Creature.GetPower<MentalConstructionPower>()
                is { } mentalConstruction)
            {
                await mentalConstruction.OnConceived(
                    choiceContext,
                    source);
            }
        }
    }

    public static async Task<int> Narrate(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        int amount)
    {
        if (amount <= 0 || Get(cardPlay.Card.Owner) < amount)
        {
            return 0;
        }

        int narrated = await Spend(
            choiceContext,
            cardPlay,
            amount);
        if (narrated > 0)
        {
            await ApplyStandardNarrateBlock(
                choiceContext,
                cardPlay,
                narrated);
        }

        return narrated;
    }

    public static async Task<int> NarrateAll(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int amount = Get(cardPlay.Card.Owner);
        if (amount <= 0)
        {
            return 0;
        }

        int narrated = await Spend(
            choiceContext,
            cardPlay,
            amount);
        if (narrated > 0)
        {
            await ApplyStandardNarrateBlock(
                choiceContext,
                cardPlay,
                narrated);
        }

        return narrated;
    }

    public static int NarratedAmount(CardPlay cardPlay)
    {
        return Narrations.TryGetValue(cardPlay, out NarrationRecord? record)
            ? record.Amount
            : 0;
    }

    public static int NarrationEffectCount(CardPlay cardPlay)
    {
        return Narrations.TryGetValue(cardPlay, out NarrationRecord? record)
            ? record.EffectCount
            : 0;
    }

    public static int NarrationEffectMultiplier(Player player) => 1;

    public static bool WasPreviousCardConceive(CardModel currentCard)
    {
        return PreviousCards.TryGetValue(
                currentCard.Owner,
                out PreviousCardRecord? record)
            && ReferenceEquals(
                record.CombatState,
                currentCard.CombatState)
            && record.WasConceive;
    }

    public static bool WasPreviousCard<T>(CardModel currentCard)
        where T : CardModel
    {
        return PreviousCards.TryGetValue(
                currentCard.Owner,
                out PreviousCardRecord? record)
            && ReferenceEquals(record.CombatState, currentCard.CombatState)
            && record.CardType == typeof(T);
    }

    private static async Task<int> Spend(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        int amount)
    {
        ThoughtPower? thought =
            cardPlay.Card.Owner.Creature.GetPower<ThoughtPower>();
        if (thought is null || thought.Amount < amount)
        {
            return 0;
        }

        await PowerCmd.ModifyAmount(
            choiceContext,
            thought,
            -amount,
            cardPlay.Card.Owner.Creature,
            cardPlay.Card,
            silent: true);

        await SyncStatePower(
            choiceContext,
            cardPlay.Card.Owner);

        int effectMultiplier =
            NarrationEffectMultiplier(cardPlay.Card.Owner);
        int effectiveAmount = amount * effectMultiplier;
        NarrationRecord record = Narrations.GetOrCreateValue(cardPlay);
        record.Amount += effectiveAmount;
        record.EffectCount += effectMultiplier;
        return effectiveAmount;
    }

    private static async Task ApplyStandardNarrateBlock(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        int amount)
    {
        await CreatureCmd.GainBlock(
            cardPlay.Card.Owner.Creature,
            amount,
            ValueProp.Unpowered,
            cardPlay);
    }

    public static async Task SyncStatePower(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player.Character is not NymphCharacter
            || player.Creature.IsDead)
        {
            return;
        }

        ThoughtState state = GetState(player);
        bool hasDesiredPower = state switch
        {
            ThoughtState.Clear =>
                player.Creature.HasPower<LucidPower>(),
            ThoughtState.Confused =>
                player.Creature.HasPower<FracturedPower>(),
            ThoughtState.Obstructed =>
                player.Creature.HasPower<ObstructedPower>(),
            _ => false
        };

        if (state != ThoughtState.Clear)
        {
            await PowerCmd.Remove<LucidPower>(player.Creature);
        }

        if (state != ThoughtState.Confused)
        {
            await PowerCmd.Remove<FracturedPower>(player.Creature);
        }

        if (state != ThoughtState.Obstructed)
        {
            await PowerCmd.Remove<ObstructedPower>(player.Creature);
        }

        if (hasDesiredPower)
        {
            return;
        }

        switch (state)
        {
            case ThoughtState.Clear:
                await PowerCmd.Apply<LucidPower>(
                    choiceContext,
                    player.Creature,
                    1,
                    player.Creature,
                    null);
                break;
            case ThoughtState.Confused:
                await PowerCmd.Apply<FracturedPower>(
                    choiceContext,
                    player.Creature,
                    1,
                    player.Creature,
                    null);
                break;
            case ThoughtState.Obstructed:
                await PowerCmd.Apply<ObstructedPower>(
                    choiceContext,
                    player.Creature,
                    1,
                    player.Creature,
                    null);
                break;
        }
    }

    public static IHoverTip CreateHoverTip()
    {
        return ModSecondaryResourceRegistry.CreateHoverTip(ResourceId);
    }

    private static NThoughtCounter CreateCounter()
    {
        NThoughtCounter counter = new();
        counter.Configure(Definition, CounterStyle);

        counter.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        counter.Position = new Vector2(85f, -350f);
        return counter;
    }

    internal static void UpdateCounter(
        NThoughtCounter counter,
        Player? player)
    {
        if (player?.Character is not NymphCharacter)
        {
            counter.BindThoughtPlayer(null);
            return;
        }

        ThoughtState state = GetState(player);
        CounterVisualState visualState =
            CounterVisualStates.GetOrCreateValue(counter);

        if (visualState.State != state)
        {
            counter.Configure(GetVisualDefinition(state), CounterStyle);
            visualState.State = state;
        }

        counter.BindThoughtPlayer(player);
    }

    private static SecondaryResourceDefinition GetVisualDefinition(
        ThoughtState state)
    {
        string iconPath = state switch
        {
            ThoughtState.Confused => ConfusedIconPath,
            ThoughtState.Obstructed => ObstructedIconPath,
            _ => ClearIconPath
        };

        return Definition with
        {
            SmallIconPath = iconPath,
            LargeIconPath = iconPath
        };
    }

    private sealed class CounterVisualState
    {
        public ThoughtState? State { get; set; }
    }

    private sealed class NarrationRecord
    {
        public int Amount { get; set; }
        public int EffectCount { get; set; }
    }

    private sealed class CardPlayThoughtStateRecord
    {
        public ThoughtState State { get; set; }
    }

    private sealed class PreviousCardRecord
    {
        public object? CombatState { get; set; }
        public bool WasConceive { get; set; }
        public Type? CardType { get; set; }
    }

    private sealed class ThoughtCardPlayListener : ICardOnPlayHookListener
    {
        public Task<bool> BeforeCardOnPlay(
            BeforeCardOnPlayContext context)
        {
            Player player = context.CardPlay.Card.Owner;
            if (player.Character is NymphCharacter)
            {
                CardPlayThoughtStates.GetOrCreateValue(
                    context.CardPlay).State = GetState(player);
            }

            return Task.FromResult(false);
        }

        public async Task AfterCardOnPlay(AfterCardOnPlayContext context)
        {
            Player player = context.CardPlay.Card.Owner;
            if (player.Character is not NymphCharacter)
            {
                return;
            }

            ThoughtState state = GetState(player, context.CardPlay);
            switch (state)
            {
                case ThoughtState.Clear:
                    player.Creature.GetPower<LucidPower>()?.Flash();
                    await CreatureCmd.GainBlock(
                        player.Creature,
                        1,
                        ValueProp.Unpowered,
                        context.CardPlay);
                    break;
                case ThoughtState.Obstructed:
                    player.Creature.GetPower<ObstructedPower>()?.Flash();
                    await CreatureCmd.Damage(
                        context.ChoiceContext,
                        player.Creature,
                        1,
                        DamageProps.nonCardUnpowered,
                        player.Creature);
                    break;
            }

            if (player.Creature.IsDead)
            {
                return;
            }

            await Create(
                context.ChoiceContext,
                player,
                1,
                context.CardPlay.Card);

            if (context.CardPlay.Card.Keywords.Contains(
                    NymphKeywords.Conceive))
            {
                await PowerCmd.Apply<ConceiveCardPlayedThisTurnPower>(
                    context.ChoiceContext,
                    player.Creature,
                    1,
                    player.Creature,
                    context.CardPlay.Card,
                    silent: true);
            }

            PreviousCardRecord record =
                PreviousCards.GetOrCreateValue(player);
            record.CombatState =
                context.CardPlay.Card.CombatState;
            record.WasConceive =
                context.CardPlay.Card.Keywords.Contains(
                    NymphKeywords.Conceive);
            record.CardType = context.CardPlay.Card.GetType();
        }
    }
}
