using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class NymphBodrakastiRitual : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/NymphUnwrittenDeed.png");

    public NymphBodrakastiRitual()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
    {
    }

#if STS2_PUBLIC
    protected override PileType GetResultPileTypeForCardPlay() =>
        PileType.Hand;
#else
    protected override CardLocation GetResultLocationForCardPlay() =>
        new(Owner, PileType.Hand, CardPilePosition.Bottom);
#endif

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        BodrakastiDistancePower? distance =
            Owner.Creature.GetPower<BodrakastiDistancePower>();
        List<CardModel> choices = [];
        if (distance?.Amount != 1)
        {
            choices.Add(Owner.Creature.CombatState!
                .CreateCard<NymphBodrakastiRetreat>(Owner));
        }

        if (distance?.Amount != 3)
        {
            choices.Add(Owner.Creature.CombatState!
                .CreateCard<NymphBodrakastiAdvance>(Owner));
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            new BlockingPlayerChoiceContext(),
            choices,
            Owner);

        if (distance is not null)
        {
            int delta = selected switch
            {
                NymphBodrakastiRetreat => -1,
                NymphBodrakastiAdvance => 1,
                _ => 0
            };
            await distance.ChangeDistance(choiceContext, delta, this);
        }

        EnergyCost.AddThisTurn(1);
    }

    protected override void OnUpgrade()
    {
    }
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class NymphBodrakastiRetreat : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/NymphDefend.png");

    public NymphBodrakastiRetreat()
        : base(-1, CardType.Skill, CardRarity.Token, TargetType.Self, false)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) => Task.CompletedTask;

    protected override void OnUpgrade()
    {
    }
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class NymphBodrakastiAdvance : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/NymphStrike.png");

    public NymphBodrakastiAdvance()
        : base(-1, CardType.Skill, CardRarity.Token, TargetType.Self, false)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) => Task.CompletedTask;

    protected override void OnUpgrade()
    {
    }
}
