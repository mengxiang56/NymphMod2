using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Nymph.Characters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Cards;

[RegisterCard(typeof(NymphCardPool))]
public sealed class NymphHeartlashCommunion : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png",
        FramePath: $"{Entry.ResPath}/images/cards/frames/bg_skill_sts2.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<NecrosisPower>(4)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>(),
        HoverTipFactory.FromPower<HeartlashCommunionPower>()
    ];

    public NymphHeartlashCommunion()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<NecrosisPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["NecrosisPower"].IntValue,
            Owner.Creature,
            this);

        if (Owner.Creature.GetPower<HeartlashCommunionPower>()
            is { } existing)
        {
            await PowerCmd.Remove(existing);
        }

        HeartlashCommunionPower power =
            (HeartlashCommunionPower)MegaCrit.Sts2.Core.Models.ModelDb
                .Power<HeartlashCommunionPower>()
                .ToMutable();
        power.ProtectedFrom = cardPlay.Target;
        await PowerCmd.Apply(
            choiceContext,
            power,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
