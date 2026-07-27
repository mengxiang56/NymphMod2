using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
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

    private static readonly SecondaryResourceCounterStyle CounterStyle = new()
    {
        CounterSize = new Vector2(150f, 100f),
        IconSize = new Vector2(150f, 100f),
        FontSize = 24,
        FormatAmount = (amount, _) => amount.ToString()
    };

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
        if (amount >= ObstructedThreshold)
        {
            return ThoughtState.Obstructed;
        }

        return amount >= ConfusedThreshold
            ? ThoughtState.Confused
            : ThoughtState.Clear;
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

    public static async Task Create(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount,
        CardModel? source = null)
    {
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

        return await Spend(
            choiceContext,
            cardPlay,
            amount);
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

        return await Spend(
            choiceContext,
            cardPlay,
            amount);
    }

    public static int NarratedAmount(CardPlay cardPlay)
    {
        return Narrations.TryGetValue(cardPlay, out NarrationRecord? record)
            ? record.Amount
            : 0;
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

        Narrations.GetOrCreateValue(cardPlay).Amount += amount;
        return amount;
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
        counter.Position = new Vector2(100f, -300f);
        return counter;
    }

    internal static void UpdateCounter(
        NThoughtCounter counter,
        Player? player)
    {
        if (player is null)
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
    }

    private sealed class ThoughtCardPlayListener : ICardOnPlayHookListener
    {
        public async Task AfterCardOnPlay(AfterCardOnPlayContext context)
        {
            Player player = context.CardPlay.Card.Owner;
            if (player.Character is not NymphCharacter)
            {
                return;
            }

            await Create(
                context.ChoiceContext,
                player,
                1,
                context.CardPlay.Card);
        }
    }
}
