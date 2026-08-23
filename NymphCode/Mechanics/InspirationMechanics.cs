using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using Nymph.Cards;
using Nymph.Characters;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Content;

namespace Nymph.Mechanics;

public static class InspirationMechanics
{
    private static readonly List<CardPileAddResult>
        PendingInitialPreviews = [];

    public const string PileStem = "Inspiration";
    public const string PileId = "NYMPH_CARDPILE_INSPIRATION";
    public const string CardLibraryFilterId = "Inspiration";
    public const string PileIconPath =
        $"{Entry.ResPath}/images/ui/inspiration_pile.png";
    public const string RewardIconPath =
        $"{Entry.ResPath}/images/ui/inspiration_reward.png";

    public static PileType PileType =>
        ModCardPileRegistry.GetPileType(PileId);

    public static void Initialize()
    {
        ModContentRegistry.For(Entry.ModId)
            .RegisterCardLibraryCompendiumSharedPoolFilter<NymphInspirationCardPool>(
                CardLibraryFilterId,
                PileIconPath);

        ModCardPileRegistry.For(Entry.ModId).RegisterOwned(
            PileStem,
            new ModCardPileSpec
            {
                Scope = ModCardPileScope.RunPersistent,
                Style = ModCardPileUiStyle.TopBarDeck,
                IconPath = PileIconPath,
                HoverTipPlacement =
                    ModCardPileHoverTipPlacement.BelowButtonTrailingEdge,
                VisibleWhen = static context =>
                    context.Player?.Character is NymphCharacter,
                View = ModCardPileViewSpec.DeckLike with
                {
                    EnableUpgradePreviewToggle = false
                }
            });
    }

    public static CardModel CreateRandomCard(
        Player player,
        CardRarity? forcedRarity = null)
    {
        var rng = player.RunState.Rng.Niche;
        CardRarity rarity;
        if (forcedRarity is { } forced)
        {
            rarity = forced;
        }
        else
        {
            int rarityRoll = rng.NextInt(100);
            rarity = rarityRoll < 10
                ? CardRarity.Rare
                : rarityRoll < 50
                    ? CardRarity.Uncommon
                    : CardRarity.Common;
        }

        List<CardModel> pool = ModelDb
            .CardPool<NymphInspirationCardPool>()
            .GetUnlockedCards(
                player.UnlockState,
                player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity == rarity)
            .ToList();
        CardModel canonical = rng.NextItem(pool)
            ?? throw new InvalidOperationException(
                $"The {rarity} inspiration card pool is empty.");
        return player.RunState.CreateCard(canonical, player);
    }

    public static async Task<CardModel> AddRandom(Player player)
    {
        CardModel card = CreateRandomCard(player);
        CardPileAddResult result = await CardPileCmd.Add(card, PileType);
        QueueInitialCardPreview(result);
        return card;
    }

    public static async Task<IReadOnlyList<CardModel>> AddRandomToPile(
        Player player,
        int amount,
        bool preview = false)
    {
        List<CardModel> added = [];
        List<CardPileAddResult> previewResults = [];
        for (int i = 0; i < amount; i++)
        {
            CardModel card = CreateRandomCard(player);
            CardPileAddResult result = await CardPileCmd.Add(card, PileType);
            if (result.success)
            {
                added.Add(result.cardAdded);
                if (preview)
                {
                    previewResults.Add(result);
                }
            }
        }

        if (previewResults.Count > 0)
        {
            Callable.From(() => PreviewInspirations(previewResults))
                .CallDeferred();
        }

        return added;
    }

    private static void QueueInitialCardPreview(
        CardPileAddResult result)
    {
        if (result.success)
        {
            PendingInitialPreviews.Add(result);
        }
    }

    public static void FlushPendingInitialPreviews()
    {
        if (NRun.Instance is null
            || PendingInitialPreviews.Count == 0)
        {
            return;
        }

        PreviewInspirations(PendingInitialPreviews);
        PendingInitialPreviews.Clear();
    }

    private static void PreviewInspirations(
        IReadOnlyList<CardPileAddResult> results)
    {
        NRun? run = NRun.Instance;
        if (run is null)
        {
            return;
        }

        foreach (CardPileAddResult result in results)
        {
            if (result.success && LocalContext.IsMine(result.cardAdded))
            {
                PreviewInspiration(run, result.cardAdded);
            }
        }
    }

    private static void PreviewInspiration(
        NRun run,
        CardModel card)
    {
        NCard? cardNode = NCard.Create(card);
        if (cardNode is null)
        {
            return;
        }

        run.GlobalUi.CardPreviewContainer.AddChild(cardNode);
        cardNode.UpdateVisuals(PileType, CardPreviewMode.Normal);

        Tween tween = cardNode.CreateTween();
        tween.TweenProperty(
                cardNode,
                "scale",
                Vector2.One,
                0.25)
            .From(Vector2.Zero)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenCallback(Callable.From(() =>
        {
            NRun? currentRun = NRun.Instance;
            if (currentRun is null ||
                !GodotObject.IsInstanceValid(cardNode))
            {
                return;
            }

            NCardFlyVfx? flyVfx = NCardFlyVfx.Create(
                cardNode,
                PileType,
                true,
                card.Owner.Character.TrailPath);
            if (flyVfx is null)
            {
                cardNode.QueueFree();
                return;
            }

            currentRun.GlobalUi.TopBar.TrailContainer
                .AddChild(flyVfx);
        })).SetDelay(1.2f);
    }

    public static async Task AddToPile(CardModel card)
    {
        await CardPileCmd.Add(card, PileType);
    }

    public static async Task OfferAtCombatStart(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        List<CardModel> options = PileType
            .GetPile(player)
            .Cards
            .ToList();
        if (options.Count == 0)
        {
            return;
        }

        CardModel? source = await SelectInspirationCard(
            choiceContext,
            options,
            player);
        if (source is null)
        {
            return;
        }

        await PlaySelectedInspiration(
            choiceContext,
            combatState,
            player,
            source);
    }

    private static async Task<CardModel?> SelectInspirationCard(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> options,
        Player player)
    {
        CardSelectorPrefs prefs = new(
            new LocString(
                "relics",
                "NYMPH_RELIC_NYMPH_RELIC.inspirationSelectionPrompt"),
            0,
            1)
        {
            Cancelable = true,
            RequireManualConfirmation = false
        };

        return (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            player,
            prefs)).FirstOrDefault();
    }

    private static async Task PlaySelectedInspiration(
        PlayerChoiceContext choiceContext,
        ICombatState combatState,
        Player player,
        CardModel source)
    {
        CardModel selected = combatState.CreateCard(
            source.CanonicalInstance,
            player);
        selected.DeckVersion = source;
        await PlayGeneratedInspiration(choiceContext, player, selected);
    }

    public static async Task PlayGeneratedInspiration(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel combatCard)
    {
        ICombatState combatState = player.Creature.CombatState
            ?? throw new InvalidOperationException(
                "Cannot play an inspiration card outside combat.");
        await CardPileCmd.AddGeneratedCardToCombat(
            combatCard,
            PileType.Hand,
            player);

        Creature? target = ResolveAutoPlayTarget(
            combatCard,
            player,
            combatState);
        await CardCmd.AutoPlay(choiceContext, combatCard, target);
        await RemoveFromCombatAfterPlayed(combatCard);
    }

    private static Creature? ResolveAutoPlayTarget(
        CardModel card,
        Player player,
        ICombatState combatState)
    {
        return card.TargetType switch
        {
            TargetType.Self => player.Creature,
            TargetType.AnyEnemy or TargetType.RandomEnemy =>
                player.RunState.Rng.CombatTargets.NextItem(
                    combatState
                        .GetOpponentsOf(player.Creature)
                        .Where(enemy => !enemy.IsDead)),
            _ => null
        };
    }

    public static void ConsumeSelected(CardModel combatCard)
    {
        CardModel? storedCard = combatCard.DeckVersion;
        if (storedCard?.Pile?.Type != PileType)
        {
            return;
        }

        storedCard.RemoveFromCurrentPile();
        combatCard.DeckVersion = null;
    }

    public static async Task RemoveFromCombatAfterPlayed(CardModel combatCard)
    {
        if (combatCard.Pile is not { IsCombatPile: true })
        {
            return;
        }

        await CardPileCmd.RemoveFromCombat(combatCard);
    }
}
