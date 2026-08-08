using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Nymph.Mechanics;
using Nymph.Rewards;

namespace Nymph.Relics;

internal static class InspirationStarterRelicSupport
{
    internal static async Task AfterObtained(Player owner)
    {
        await InspirationMechanics.AddRandom(owner);
    }

    internal static async Task BeforeCombatStart(Player owner)
    {
        await ThoughtMechanics.SyncStatePower(
            new ThrowingPlayerChoiceContext(),
            owner);
    }

    internal static async Task OfferCombatStartInspiration(
        PlayerChoiceContext choiceContext,
        Player player,
        ICombatState combatState)
    {
        await InspirationMechanics.OfferAtCombatStart(
            player,
            choiceContext,
            combatState);
    }

    internal static bool TryModifyRewards(
        Player player,
        List<Reward> rewards,
        AbstractRoom? room,
        bool guaranteedDrop)
    {
        if (room is not CombatRoom)
        {
            return false;
        }

        if (room.RoomType == RoomType.Boss
            && IsLastAct(player))
        {
            return false;
        }

        if (room.RoomType == RoomType.Boss)
        {
            rewards.Add(new InspirationReward(player, CardRarity.Rare));
            return true;
        }

        if (room.RoomType is not (RoomType.Monster or RoomType.Elite))
        {
            return false;
        }

        if (!guaranteedDrop)
        {
            return false;
        }

        rewards.Add(new InspirationReward(player));
        return true;
    }

    private static bool IsLastAct(Player player) =>
        player.RunState.CurrentActIndex == player.RunState.Acts.Count - 1;
}
