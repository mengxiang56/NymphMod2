using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Nymph.Cards;
using STS2RitsuLib.CardPiles;

namespace Nymph.Mechanics;

public static class InspirationMechanics
{
    public const string PileStem = "Inspiration";
    public const string PileId = "NYMPH_CARDPILE_INSPIRATION";
    public const string PileIconPath =
        $"{Entry.ResPath}/images/ui/inspiration_pile.png";

    private static readonly Type[] CommonCards =
    [
        typeof(NymphInspirationMercenary),
        typeof(NymphInspirationPillage),
        typeof(NymphInspirationOutblood),
        typeof(NymphInspirationRest)
    ];

    private static readonly Type[] UncommonCards =
    [
        typeof(NymphInspirationCivilWar),
        typeof(NymphInspirationWall),
        typeof(NymphInspirationInvasion),
        typeof(NymphInspirationCatastrophe),
        typeof(NymphInspirationOathbreak),
        typeof(NymphInspirationFlames),
        typeof(NymphInspirationSleep)
    ];

    private static readonly Type[] RareCards =
    [
        typeof(NymphInspirationRelocation),
        typeof(NymphInspirationMarch),
        typeof(NymphInspirationFurnace)
    ];

    public static PileType PileType =>
        ModCardPileRegistry.GetPileType(PileId);

    public static void Initialize()
    {
        ModCardPileRegistry.For(Entry.ModId).RegisterOwned(
            PileStem,
            new ModCardPileSpec
            {
                Scope = ModCardPileScope.RunPersistent,
                Style = ModCardPileUiStyle.TopBarDeck,
                IconPath = PileIconPath,
                View = ModCardPileViewSpec.DeckLike with
                {
                    EnableUpgradePreviewToggle = false
                }
            });
    }

    public static async Task<CardModel> AddRandom(Player player)
    {
        var rng = player.RunState.Rng.Niche;
        int rarityRoll = rng.NextInt(100);
        Type[] pool = rarityRoll < 50
            ? CommonCards
            : rarityRoll < 90
                ? UncommonCards
                : RareCards;
        Type selectedType = rng.NextItem(pool)
            ?? throw new InvalidOperationException(
                "The inspiration card pool is empty.");
        CardModel canonical = ModelDb.GetById<CardModel>(
            ModelDb.GetId(selectedType));
        CardModel card = player.RunState.CreateCard(canonical, player);
        await CardPileCmd.Add(card, PileType);
        return card;
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

        CardSelectorPrefs prefs = new(
            new LocString(
                "relics",
                "NYMPH_RELIC_NYMPH_RELIC.inspirationSelectionPrompt"),
            0,
            1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        CardModel? source = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            player,
            prefs)).FirstOrDefault();
        if (source is not null)
        {
            CardModel selected = combatState.CreateCard(
                source.CanonicalInstance,
                player);
            selected.DeckVersion = source;
            await CardPileCmd.AddGeneratedCardToCombat(
                selected,
                PileType.Hand,
                player);
        }
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
}
