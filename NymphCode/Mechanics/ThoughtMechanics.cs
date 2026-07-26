using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using System.Runtime.CompilerServices;
using STS2RitsuLib.Cards;
using STS2RitsuLib.Combat.SecondaryResources;
using Nymph.Characters;

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

    private const string ClearIconPath =
        $"{Entry.ResPath}/images/ui/thought/thought_clear.png";
    private const string ConfusedIconPath =
        $"{Entry.ResPath}/images/ui/thought/thought_confused.png";
    private const string ObstructedIconPath =
        $"{Entry.ResPath}/images/ui/thought/thought_obstructed.png";

    private static bool _initialized;
    private static readonly ConditionalWeakTable<
        NSecondaryResourceCounter,
        CounterVisualState> CounterVisualStates = new();

    private static readonly SecondaryResourceCounterStyle CounterStyle = new()
    {
        CounterSize = new Vector2(104f, 64f),
        IconSize = new Vector2(86f, 60f),
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
                hardMaxAmount: 999,
                persistencePolicy: SecondaryResourcePersistencePolicy.Combat,
                locTable: "static_hover_tips",
                titleKey: "NYMPH_SECONDARY_RESOURCE_THOUGHT.title",
                descriptionKey: "NYMPH_SECONDARY_RESOURCE_THOUGHT.description",
                smallIconPath: ClearIconPath,
                largeIconPath: ClearIconPath));

        registry.AlwaysShowInCombatUiForCharacter<NymphCharacter>(LocalId);
        registry.RegisterCombatUi<NSecondaryResourceCounter>(
            "ThoughtCounter",
            _ => CreateCounter(),
            context => UpdateCounter(context.Node, context.Player));

        SecondaryResourcePersistence.Initialize();
        CardOnPlayHook.RegisterGlobalListener(new ThoughtCardPlayListener());
    }

    public static int Get(Player player)
    {
        return SecondaryResourceCmd.Get(player, ResourceId);
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

    public static Task<int> Create(Player player, int amount, object? source = null)
    {
        return SecondaryResourceCmd.Gain(
            player,
            ResourceId,
            amount,
            source as MegaCrit.Sts2.Core.Models.AbstractModel);
    }

    public static IHoverTip CreateHoverTip()
    {
        return ModSecondaryResourceRegistry.CreateHoverTip(ResourceId);
    }

    private static NSecondaryResourceCounter CreateCounter()
    {
        NSecondaryResourceCounter counter = NSecondaryResourceCounter.Create(
            Definition,
            CounterStyle);

        counter.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        counter.Position = new Vector2(96f, -190f);
        return counter;
    }

    private static void UpdateCounter(
        NSecondaryResourceCounter counter,
        Player? player)
    {
        if (player is null)
        {
            counter.Bind(null);
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

        counter.Bind(player);
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

    private sealed class ThoughtCardPlayListener : ICardOnPlayHookListener
    {
        public async Task AfterCardOnPlay(AfterCardOnPlayContext context)
        {
            Player player = context.CardPlay.Card.Owner;
            if (player.Character is not NymphCharacter)
            {
                return;
            }

            await SecondaryResourceCmd.Gain(
                player,
                ResourceId,
                1,
                context.CardPlay.Card);
        }
    }
}
