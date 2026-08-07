using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Nymph.Cards;
using Nymph.Characters;
using Nymph.Powers;

namespace Nymph.Mechanics;

public static class RecreateMechanics
{
    private static readonly BlockingPlayerChoiceContext
        AutoPlayChoiceContext = new();

    private static LocString SelectionPromptExact => new(
        "static_hover_tips",
        "NYMPH_RECREATE_SELECTION_PROMPT");

    private static LocString SelectionPromptUpTo => new(
        "static_hover_tips",
        "NYMPH_RECREATE_SELECTION_PROMPT_UP_TO");

    private static CardSelectorPrefs CreateSelectorPrefs(
        int minCount,
        int maxCount)
    {
        LocString prompt = minCount < maxCount
            ? SelectionPromptUpTo
            : SelectionPromptExact;

        return new CardSelectorPrefs(prompt, minCount, maxCount)
        {
            Cancelable = minCount == 0
        };
    }

    public static async Task<IReadOnlyList<RecreateResult>> SelectFromHand(
        PlayerChoiceContext choiceContext,
        CardModel source,
        int minCount,
        int maxCount,
        bool makeFreeUntilPlayed = false,
        bool applyAdaptability = true)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        CardSelectorPrefs prefs = CreateSelectorPrefs(minCount, maxCount);

        IReadOnlyList<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            source.Owner,
            prefs,
            card => card.IsTransformable,
            source)).ToList();

        return await Recreate(
            selected,
            makeFreeUntilPlayed,
            null,
            applyAdaptability);
    }

    public static async Task<IReadOnlyList<RecreateResult>> SelectFromHand(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        AbstractModel source,
        int minCount,
        int maxCount,
        bool makeFreeUntilPlayed = false,
        bool applyAdaptability = true)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        CardSelectorPrefs prefs = CreateSelectorPrefs(minCount, maxCount);

        IReadOnlyList<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            prefs,
            card => card.IsTransformable,
            source)).ToList();

        return await Recreate(
            selected,
            makeFreeUntilPlayed,
            null,
            applyAdaptability);
    }

    public static async Task AutoPlayReplacements(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<RecreateResult> results)
    {
        foreach (RecreateResult result in results)
        {
            CardModel card = result.Replacement;
            if (card.Pile?.Type != PileType.Hand)
            {
                continue;
            }

            Creature? target = card.TargetType switch
            {
                TargetType.Self => card.Owner.Creature,
                TargetType.AnyEnemy or TargetType.RandomEnemy =>
                    card.Owner.RunState.Rng.CombatTargets.NextItem(
                        card.CombatState!
                            .GetOpponentsOf(card.Owner.Creature)
                            .Where(enemy => !enemy.IsDead)),
                _ => null
            };
            if (card.TargetType is TargetType.AnyEnemy
                    or TargetType.RandomEnemy
                && target is null)
            {
                continue;
            }

            await CardCmd.AutoPlay(choiceContext, card, target);
        }
    }

    public static async Task<IReadOnlyList<RecreateResult>> RandomFromHand(
        CardModel source,
        int count,
        bool makeFreeUntilPlayed = false)
    {
        List<CardModel> candidates = PileType.Hand
            .GetPile(source.Owner)
            .Cards
            .Where(card => card.IsTransformable)
            .ToList();
        List<CardModel> selected = [];

        while (selected.Count < count && candidates.Count > 0)
        {
            CardModel card = source.Owner.RunState.Rng
                .CombatCardSelection
                .NextItem(candidates)
                ?? throw new InvalidOperationException(
                    "Failed to select a card to recreate.");
            candidates.Remove(card);
            selected.Add(card);
        }

        return await Recreate(
            selected,
            makeFreeUntilPlayed,
            null,
            applyAdaptability: true);
    }

    public static Task<IReadOnlyList<RecreateResult>> Cards(
        IReadOnlyList<CardModel> originals,
        bool makeFreeUntilPlayed = false)
    {
        return Recreate(
            originals,
            makeFreeUntilPlayed,
            null,
            applyAdaptability: true);
    }

    public static Task<IReadOnlyList<RecreateResult>> CardsIntoPool(
        IReadOnlyList<CardModel> originals,
        Func<CardModel, bool> replacementFilter)
    {
        return Recreate(
            originals,
            makeFreeUntilPlayed: false,
            replacementFilter,
            applyAdaptability: true);
    }

    public static async Task<RecreateResult> CreateReplacementInHand(
        CardModel original,
        Func<CardModel, bool>? replacementFilter = null,
        bool applyAdaptability = true)
    {
        RegisterMysteryOfSmelting(original);
        RecreateResult result = CreateResult(
            original,
            makeFreeUntilPlayed: false,
            replacementFilter);
        await CardPileCmd.AddGeneratedCardToCombat(
            result.Replacement,
            PileType.Hand,
            original.Owner);
        if (applyAdaptability)
        {
            AdaptabilityPower.EnchantRecreatedCards(
                original.Owner,
                [result]);
        }

        ImpressionReconstructionPower.ReduceRecreatedCardCosts(
            original.Owner,
            [result]);

        NymphSeeThroughPast.AddRecreatedAttackDamage(
            original.Owner,
            [result]);

        await AutoPlayNewBranches([result]);

        return result;
    }

    public static async Task<IReadOnlyList<RecreateResult>>
        SelectFromHandIntoPool(
            PlayerChoiceContext choiceContext,
        CardModel source,
        int minCount,
        int maxCount,
        Func<CardModel, bool> replacementFilter,
        bool applyAdaptability = true)
    {
        CardSelectorPrefs prefs = CreateSelectorPrefs(minCount, maxCount);

        IReadOnlyList<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            source.Owner,
            prefs,
            card => card.IsTransformable,
            source)).ToList();

        return await Recreate(
            selected,
            makeFreeUntilPlayed: false,
            replacementFilter,
            applyAdaptability);
    }

    public static async Task<IReadOnlyList<RecreateResult>>
        PermanentlySelectFromHand(
            PlayerChoiceContext choiceContext,
            CardModel source,
            int minCount,
            int maxCount)
    {
        CardSelectorPrefs prefs = CreateSelectorPrefs(minCount, maxCount);

        IReadOnlyList<CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext,
            source.Owner,
            prefs,
            card =>
                card.IsTransformable
                && card.DeckVersion is { IsTransformable: true }
                    deckVersion
                && !deckVersion.Keywords.Contains(
                    CardKeyword.Eternal),
            source)).ToList();
        List<RecreateResult> results = [];

        foreach (CardModel combatOriginal in selected)
        {
            RegisterMysteryOfSmelting(combatOriginal);
            CardModel deckOriginal = combatOriginal.DeckVersion!;
            RecreateResult deckResult = CreateResult(
                deckOriginal,
                makeFreeUntilPlayed: false,
                replacementFilter: null);

            await CardCmd.Transform(
                deckOriginal,
                deckResult.Replacement,
                CardPreviewStyle.None);

            CardModel combatReplacement =
                combatOriginal.CombatState!.CreateCard(
                    deckResult.Replacement.CanonicalInstance,
                    source.Owner);
            for (int i = 0;
                 i < deckResult.Replacement.CurrentUpgradeLevel
                 && combatReplacement.IsUpgradable;
                 i++)
            {
                CardCmd.Upgrade(
                    combatReplacement,
                    CardPreviewStyle.None);
            }

            combatReplacement.DeckVersion =
                deckResult.Replacement;
            await CardCmd.Transform(
                combatOriginal,
                combatReplacement,
                CardPreviewStyle.HorizontalLayout);
            results.Add(
                new RecreateResult(
                    combatOriginal,
                    combatReplacement,
                    deckResult.OriginalEnergyCost));
        }

        AdaptabilityPower.EnchantRecreatedCards(
            source.Owner,
            results);
        ImpressionReconstructionPower.ReduceRecreatedCardCosts(
            source.Owner,
            results);
        NymphSeeThroughPast.AddRecreatedAttackDamage(
            source.Owner,
            results);
        await AutoPlayNewBranches(results);
        return results;
    }

    private static async Task<IReadOnlyList<RecreateResult>> Recreate(
        IReadOnlyList<CardModel> originals,
        bool makeFreeUntilPlayed,
        Func<CardModel, bool>? replacementFilter,
        bool applyAdaptability)
    {
        if (originals.Count == 0)
        {
            return [];
        }

        List<CardTransformation> transformations = [];
        List<RecreateResult> results = [];
        Dictionary<CardModel, NCard> playedCardNodes = [];

        foreach (CardModel original in originals)
        {
            RegisterMysteryOfSmelting(original);
            if (original.Pile?.Type == PileType.Play
                && NCard.FindOnTable(original) is { } playedCardNode)
            {
                playedCardNodes[original] = playedCardNode;
            }

            RecreateResult result = CreateResult(
                original,
                makeFreeUntilPlayed,
                replacementFilter);

            transformations.Add(
                new CardTransformation(
                    result.Original,
                    result.Replacement));
            results.Add(result);
        }

        await CardCmd.Transform(
            transformations,
            null,
            CardPreviewStyle.HorizontalLayout);

        foreach (RecreateResult result in results)
        {
            if (result.Replacement.Pile?.Type == PileType.Play
                && playedCardNodes.TryGetValue(
                    result.Original,
                    out NCard? playedCardNode))
            {
                playedCardNode.Model = result.Replacement;
            }

            if (result.Replacement.Pile?.Type != PileType.Hand)
            {
                await CardPileCmd.Add(
                    result.Replacement,
                    PileType.Hand);
            }
        }

        if (applyAdaptability)
        {
            AdaptabilityPower.EnchantRecreatedCards(
                originals[0].Owner,
                results);
        }

        ImpressionReconstructionPower.ReduceRecreatedCardCosts(
            originals[0].Owner,
            results);

        NymphSeeThroughPast.AddRecreatedAttackDamage(
            originals[0].Owner,
            results);

        if (originals[0].Owner.Creature.GetPower<NemesisUndertakerPower>() is { } undertaker)
        {
            await undertaker.OnRecreated(AutoPlayChoiceContext, results);
        }

        await AutoPlayNewBranches(results);

        return results;
    }

    private static async Task AutoPlayNewBranches(
        IEnumerable<RecreateResult> results)
    {
        foreach (NymphNewBranch card in results
            .Select(result => result.Replacement)
            .OfType<NymphNewBranch>()
            .Where(card => card.Pile?.Type == PileType.Hand)
            .ToList())
        {
            await NymphNewBranch.TryAutoPlayIfAllowed(
                AutoPlayChoiceContext,
                card);
        }
    }

    private static void RegisterMysteryOfSmelting(CardModel original)
    {
        if (original is NymphMysteryOfSmelting
            {
                CombatState: not null,
                DeckVersion: NymphMysteryOfSmelting deckVersion
            })
        {
            deckVersion.AutoPlayNextCombat = true;
        }
    }

    private static RecreateResult CreateResult(
        CardModel original,
        bool makeFreeUntilPlayed,
        Func<CardModel, bool>? replacementFilter)
    {
        int energyCost = original.EnergyCost.CostsX
            ? 0
            : original.EnergyCost.GetWithModifiers(CostModifiers.All);
        IEnumerable<CardModel> nymphCardPool =
            ModelDb.CardPool<NymphCardPool>().GetUnlockedCards(
                original.Owner.UnlockState,
                original.Owner.RunState.CardMultiplayerConstraint)
            .Where(card =>
                (card.Type is CardType.Attack
                    or CardType.Skill
                    or CardType.Power)
                && (card.Rarity is CardRarity.Common
                    or CardRarity.Uncommon
                    or CardRarity.Rare)
                && card.CanBeGeneratedInCombat);
        if (replacementFilter is not null)
        {
            nymphCardPool = nymphCardPool.Where(replacementFilter);
        }

        CardModel replacement =
            original is NymphMasterlessMemories
                ? original.CombatState!
                    .CreateCard<NymphBagOfIdeas>(original.Owner)
                : original is NymphNarrativeAnchor
                    ? original.CombatState!
                        .CreateCard<NymphNarrativeAnchor>(original.Owner)
                : new CardTransformation(
                    original,
                    nymphCardPool).GetReplacement(
                    original.Owner.PlayerRng.Transformations)!;

        int targetUpgradeLevel = original is NymphNarrativeAnchor
            ? original.CurrentUpgradeLevel + 1
            : original.CurrentUpgradeLevel;
        for (int i = 0;
             i < targetUpgradeLevel && replacement.IsUpgradable;
             i++)
        {
            CardCmd.Upgrade(
                replacement,
                CardPreviewStyle.None);
        }

        if (makeFreeUntilPlayed)
        {
            replacement.EnergyCost.SetUntilPlayed(0);
            replacement.SetStarCostUntilPlayed(0);
        }

        if (original.Owner.Creature.GetPower<FutureLongingPower>()
            is { } futureLonging)
        {
            foreach (DynamicVar variable in replacement.DynamicVars.Values)
            {
                if (variable.Name == "Energy")
                {
                    continue;
                }

                variable.BaseValue += futureLonging.Amount;
                variable.ResetToBase();
            }

            futureLonging.FlashForRecreate();
        }

        return new RecreateResult(
            original,
            replacement,
            energyCost);
    }
}

public sealed record RecreateResult(
    CardModel Original,
    CardModel Replacement,
    int OriginalEnergyCost);
