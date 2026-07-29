using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphCrownOfChieftain : ModCardTemplate
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust,
        NymphKeywords.Conceive
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Create", 4),
        new CardsVar(2),
        new BlockVar(4, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move)
    ];

    public NymphCrownOfChieftain()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await ThoughtMechanics.Create(
            choiceContext,
            Owner,
            DynamicVars["Create"].IntValue,
            this);

        List<CardModel> candidates = PileType.Hand
            .GetPile(Owner)
            .Cards
            .Where(card => card.IsUpgradable)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        int maxCount = Math.Min(
            DynamicVars.Cards.IntValue,
            candidates.Count);
        CardSelectorPrefs prefs = new(
            SelectionScreenPrompt,
            0,
            maxCount)
        {
            Cancelable = true
        };
        IReadOnlyList<CardModel> selected =
            (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                prefs,
                card => card.IsUpgradable,
                this)).ToList();

        foreach (CardModel card in selected)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
            await CreatureCmd.GainBlock(
                Owner.Creature,
                DynamicVars.Block,
                cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
