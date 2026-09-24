using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class NymphHolyCityShield : ModCardTemplate
{
    private int _timesPlayed;

    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("RemainingPlays", 3)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath:
            $"{Entry.ResPath}/images/cards/NymphHolyCityShield.png");

    public NymphHolyCityShield()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.Self, false)
    {
    }

    [SavedProperty]
    public int TimesPlayed
    {
        get => _timesPlayed;
        set
        {
            AssertMutable();
            _timesPlayed = value;
            DynamicVars["RemainingPlays"].BaseValue =
                Math.Max(0, 3 - value);
        }
    }

#if STS2_PUBLIC
    protected override PileType GetResultPileTypeForCardPlay() =>
        TimesPlayed >= 2 ? PileType.Exhaust : PileType.Hand;
#else
    protected override CardLocation GetResultLocationForCardPlay() =>
        new(
            Owner,
            TimesPlayed >= 2 ? PileType.Exhaust : PileType.Hand,
            CardPilePosition.Bottom);
#endif

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        TimesPlayed++;
        await MegaCrit.Sts2.Core.Commands.PowerCmd
            .Apply<HolyCityEmbracePower>(
                choiceContext,
                Owner.Creature,
                10,
                Owner.Creature,
                this);
    }

    protected override void OnUpgrade()
    {
    }
}
