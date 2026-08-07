using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Potions;

[RegisterPotion(typeof(NymphPotionPool))]
public sealed class NymphRecreatePotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NymphKeywords.RecreateId
    ];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath:
            $"{Entry.ResPath}/images/potions/NymphRecreatePotion.png",
        OutlinePath:
            $"{Entry.ResPath}/images/potions/NymphRecreatePotionOutline.png");

    protected override async Task OnUse(
        PlayerChoiceContext choiceContext,
        Creature? target)
    {
        AssertValidForTargetedPotion(target);
        NCombatRoom.Instance?.PlaySplashVfx(
            target,
            new Color("24202e"));

        Creature selectedTarget = target!;
        int maxCount = PileType.Hand
            .GetPile(selectedTarget.Player!)
            .Cards
            .Count(card => card.IsTransformable);
        await RecreateMechanics.SelectFromHand(
            choiceContext,
            selectedTarget.Player!,
            this,
            0,
            maxCount);
    }
}
