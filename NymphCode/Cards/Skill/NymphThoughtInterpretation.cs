using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Characters;
using Nymph.Mechanics;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
[RegisterCharacterStarterCard(typeof(NymphCharacter), 1)]
public sealed class NymphThoughtInterpretation : ModCardTemplate
{
    private const string NarrateUseId = "NymphThoughtInterpretation.Narrate";

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        NymphKeywords.Narrate
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4m, ValueProp.Move)
        , new DynamicVar("Narrate", 5)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ThoughtMechanics.CreateHoverTip()
    ];

    public NymphThoughtInterpretation()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self, true)
    {
        this.SecondaryResourceUses().SpendIfAvailable(
            NarrateUseId,
            ThoughtMechanics.ResourceId,
            DynamicVars["Narrate"].IntValue);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        int narrated = cardPlay.SecondaryResources()
            .SpentByUse(NarrateUseId);
        if (narrated > 0)
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                narrated,
                ValueProp.Unpowered,
                cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
