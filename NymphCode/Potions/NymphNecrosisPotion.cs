using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Nymph.Characters;
using Nymph.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Potions;

[RegisterPotion(typeof(NymphPotionPool))]
public sealed class NymphNecrosisPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<NecrosisPower>(3)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<NecrosisPower>()
    ];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath:
            $"{Entry.ResPath}/images/potions/NymphNecrosisPotion.png",
        OutlinePath:
            $"{Entry.ResPath}/images/potions/NymphNecrosisPotionOutline.png");

    protected override async Task OnUse(
        PlayerChoiceContext choiceContext,
        Creature? target)
    {
        AssertValidForTargetedPotion(target);
        Creature selectedTarget = target!;
        NCombatRoom.Instance?.PlaySplashVfx(
            selectedTarget,
            new Color("8c5bb5"));

        await PowerCmd.Apply<NecrosisPower>(
            choiceContext,
            selectedTarget,
            DynamicVars["NecrosisPower"].BaseValue,
            Owner.Creature,
            null);
    }
}
