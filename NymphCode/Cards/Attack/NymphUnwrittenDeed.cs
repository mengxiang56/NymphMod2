using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphDerivedCardPool))]
public sealed class NymphUnwrittenDeed : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Ethereal,
        CardKeyword.Exhaust,
        NymphKeywords.Conceive
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_attack_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20, ValueProp.Move),
        new DynamicVar("Create", 8)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<NymphPureWhitePetal>(),
        HoverTipFactory.FromCard<NymphBabelOath>()
    ];

    public NymphUnwrittenDeed()
        : base(2, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await ThoughtMechanics.Create(
            choiceContext,
            Owner,
            DynamicVars["Create"].IntValue,
            this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (!ThoughtMechanics.WasPreviousCard<NymphPureWhitePetal>(this))
        {
            return;
        }

        NymphBabelOath oath =
            CombatState!.CreateCard<NymphBabelOath>(Owner);
        PileType destination =
            IsUpgraded ? PileType.Hand : PileType.Draw;
        CardPileAddResult result =
            await CardPileCmd.AddGeneratedCardToCombat(
                oath,
                destination,
                Owner,
                IsUpgraded
                    ? CardPilePosition.Bottom
                    : CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(result);
    }

    protected override void OnUpgrade()
    {
    }
}
