using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Mechanics;
using Nymph.Patches;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(StatusCardPool))]
public sealed class NymphExiledBlackCoffin : ModCardTemplate
{
    private SerializableCard? _originalCard;
    private List<SerializableCard> _originalDeckVersions = [];

    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable,
        NymphKeywords.Recreate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath:
            $"{Entry.ResPath}/images/cards/NymphExiledBlackCoffin.png");

    [SavedProperty]
    public SerializableCard? OriginalCard
    {
        get => _originalCard;
        set
        {
            AssertMutable();
            _originalCard = value;
        }
    }

    [SavedProperty]
    public List<SerializableCard> OriginalDeckVersions
    {
        get => _originalDeckVersions;
        set
        {
            AssertMutable();
            _originalDeckVersions = value;
        }
    }

    public NymphExiledBlackCoffin()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.None, false)
    {
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add(
            "OriginalCardName",
            OriginalCard is null
                ? string.Empty
                : CardModel.FromSerializable(OriginalCard).Title);
    }

    internal async Task RestoreOriginal()
    {
        if (OriginalCard is null
            || CardScope is null
            || Pile?.Type != PileType.Hand)
        {
            return;
        }

        CardModel restored = CardModel.FromSerializable(OriginalCard);
        CardScope.AddCard(restored, Owner);
        SerializableCard? deckSnapshot = OriginalDeckVersions.FirstOrDefault();
        if (deckSnapshot is not null)
        {
            restored.DeckVersion = Owner.Deck.Cards.FirstOrDefault(card =>
                MatchesSnapshot(card.ToSerializable(), deckSnapshot));
        }

        FremontBlackCoffinPatch.BeginInternalTransform(this);
        try
        {
            await CardCmd.Transform(
                this,
                restored,
                CardPreviewStyle.None);
        }
        finally
        {
            FremontBlackCoffinPatch.EndInternalTransform(this);
        }
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) => Task.CompletedTask;

    private static bool MatchesSnapshot(
        SerializableCard candidate,
        SerializableCard expected)
    {
        return candidate.Id == expected.Id
            && candidate.CurrentUpgradeLevel == expected.CurrentUpgradeLevel
            && Equals(candidate.Enchantment, expected.Enchantment)
            && candidate.FloorAddedToDeck == expected.FloorAddedToDeck
            && string.Equals(
                candidate.Props?.ToString(),
                expected.Props?.ToString(),
                StringComparison.Ordinal);
    }

    protected override void OnUpgrade()
    {
    }
}
