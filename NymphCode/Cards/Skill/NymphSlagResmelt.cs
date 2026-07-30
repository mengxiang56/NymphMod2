using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphSlagResmelt : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.Recreate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Recreate", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<Fuel>(),
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];

    public NymphSlagResmelt()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        IReadOnlyList<RecreateResult> recreated =
            await RecreateMechanics.SelectFromHand(
                choiceContext,
                this,
                DynamicVars["Recreate"].IntValue,
                DynamicVars["Recreate"].IntValue);

        int fuelCount =
            recreated.Sum(result => result.OriginalEnergyCost);
        for (int i = 0; i < fuelCount; i++)
        {
            CardModel fuel =
                CombatState!.CreateCard<Fuel>(Owner);
            if (IsUpgraded)
            {
                fuel.AddKeyword(CardKeyword.Retain);
            }

            await CardPileCmd.AddGeneratedCardToCombat(
                fuel,
                PileType.Hand,
                Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
