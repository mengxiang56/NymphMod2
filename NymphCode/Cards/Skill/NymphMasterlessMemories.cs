using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphMasterlessMemories : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.RecreateId),
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.InspirationId),
        HoverTipFactory.FromCard<NymphBagOfIdeas>(IsUpgraded)
    ];

    public NymphMasterlessMemories()
        : base(-1, CardType.Skill, CardRarity.Uncommon, TargetType.None, true)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (card != this
            || oldPileType != PileType.None
            || card.Pile?.Type != PileType.Deck)
        {
            return;
        }

        await InspirationMechanics.AddRandomToPile(
            Owner,
            DynamicVars.Cards.IntValue,
            preview: true);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
