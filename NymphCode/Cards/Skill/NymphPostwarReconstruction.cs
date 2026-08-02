using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphPostwarReconstruction : ModCardTemplate
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.Recreate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4, ValueProp.Unpowered),
        new DynamicVar("Threshold", 2)
    ];

    public NymphPostwarReconstruction()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        List<CardModel> cards = PileType.Hand
            .GetPile(Owner)
            .Cards
            .Where(card =>
                card.Type != CardType.Attack
                && card.IsTransformable)
            .ToList();
        IReadOnlyList<RecreateResult> recreated =
            await RecreateMechanics.CardsIntoPool(
                cards,
                card => card.Type == CardType.Attack);
        if (recreated.Count == 0)
        {
            return;
        }

        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block.BaseValue * recreated.Count,
            ValueProp.Unpowered,
            cardPlay);
        await ThoughtMechanics.IncreaseConfusedThreshold(
            choiceContext,
            Owner,
            DynamicVars["Threshold"].IntValue * recreated.Count,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars["Threshold"].UpgradeValueBy(1);
    }
}
