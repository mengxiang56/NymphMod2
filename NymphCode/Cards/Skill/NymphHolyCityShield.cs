using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class NymphHolyCityShield : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/NymphHolyCityEmbrace.png");

    public NymphHolyCityShield()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.Self, false)
    {
    }

#if STS2_PUBLIC
    protected override PileType GetResultPileTypeForCardPlay() =>
        PileType.Hand;
#else
    protected override CardLocation GetResultLocationForCardPlay() =>
        new(Owner, PileType.Hand, CardPilePosition.Bottom);
#endif

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        MegaCrit.Sts2.Core.Commands.PowerCmd
            .Apply<HolyCityEmbracePower>(
                choiceContext,
                Owner.Creature,
                10,
                Owner.Creature,
                this);

    protected override void OnUpgrade()
    {
    }
}
