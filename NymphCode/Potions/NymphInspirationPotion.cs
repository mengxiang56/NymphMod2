using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Nymph.Characters;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Potions;

[RegisterPotion(typeof(NymphPotionPool))]
public sealed class NymphInspirationPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NymphKeywords.InspirationId
    ];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath:
            $"{Entry.ResPath}/images/potions/NymphInspirationPotion.png",
        OutlinePath:
            $"{Entry.ResPath}/images/potions/NymphInspirationPotionOutline.png");

    protected override async Task OnUse(
        PlayerChoiceContext choiceContext,
        Creature? target)
    {
        AssertValidForTargetedPotion(target);
        Player player = target!.Player!;
        NCombatRoom.Instance?.PlaySplashVfx(
            target,
            new Color("5a8fd4"));

        List<CardModel> options = CardFactory
            .GetDistinctForCombat(
                player,
                ModelDb
                    .CardPool<NymphInspirationCardPool>()
                    .GetUnlockedCards(
                        player.UnlockState,
                        player.RunState.CardMultiplayerConstraint),
                3,
                player.RunState.Rng.CombatCardGeneration)
            .ToList();
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            options,
            player,
            canSkip: true);
        if (selected is null)
        {
            return;
        }

        await InspirationMechanics.PlayGeneratedInspiration(
            choiceContext,
            player,
            selected);
    }
}
