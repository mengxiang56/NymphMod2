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
        new PowerVar<NecrosisPower>(4),
        new DynamicVar("Turns", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>()
    ];

    public NymphHeartlashCommunion()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy, true)
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

        HeartlashCommunionPower? existing = Owner.Creature
            .GetPowerInstances<HeartlashCommunionPower>()
            .FirstOrDefault(power =>
                power.ProtectedFrom == cardPlay.Target);
        if (existing is not null)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                existing,
                DynamicVars["Turns"].IntValue,
                Owner.Creature,
                this);
            return;
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
            DynamicVars["Turns"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
