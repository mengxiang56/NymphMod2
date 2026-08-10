using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphBagOfIdeas : ModCardTemplate
{
    private const int MaxDurability = 3;
    private const int UpgradedEnergyGain = 1;
    private int _durabilityRemaining = MaxDurability;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.Conceive,
        CardKeyword.Innate
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Durability", MaxDurability),
        new DynamicVar("Create", 4),
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        new HoverTip(
            new LocString(
                "static_hover_tips",
                "NYMPH_DURABILITY.title"),
            new LocString(
                "static_hover_tips",
                "NYMPH_DURABILITY.description"))
    ];

    [SavedProperty]
    public int DurabilityRemaining
    {
        get => _durabilityRemaining;
        set
        {
            AssertMutable();
            _durabilityRemaining = Math.Max(0, value);
            DynamicVars["Durability"].BaseValue =
                _durabilityRemaining;
        }
    }

    public NymphBagOfIdeas()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

#if STS2_PUBLIC
    protected override PileType GetResultPileTypeForCardPlay()
    {
        return DurabilityRemaining <= 1
            ? PileType.None
            : base.GetResultPileTypeForCardPlay();
    }
#else
    protected override CardLocation GetResultLocationForCardPlay()
    {
        return DurabilityRemaining <= 1
            ? new CardLocation(
                Owner,
                PileType.None,
                CardPilePosition.Bottom)
            : base.GetResultLocationForCardPlay();
    }
#endif

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        SetDurability(this, DurabilityRemaining - 1);
        if (DeckVersion is NymphBagOfIdeas deckVersion)
        {
            SetDurability(
                deckVersion,
                DurabilityRemaining);
            if (DurabilityRemaining <= 0)
            {
                await CardPileCmd.RemoveFromDeck(deckVersion);
            }
        }

        await ThoughtMechanics.Create(
            choiceContext,
            Owner,
            DynamicVars["Create"].IntValue,
            this);
        if (IsUpgraded)
        {
            await PlayerCmd.GainEnergy(UpgradedEnergyGain, Owner);
        }

        await CardPileCmd.Draw(
            choiceContext,
            DynamicVars.Cards.IntValue,
            Owner);
    }

    private static void SetDurability(
        NymphBagOfIdeas card,
        int amount)
    {
        card.DurabilityRemaining = amount;
    }

    protected override void OnUpgrade()
    {
    }
}
