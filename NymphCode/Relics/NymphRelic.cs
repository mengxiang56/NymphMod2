using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Rewards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
[RegisterCharacterStarterRelic(typeof(NymphCharacter))]
public sealed class NymphRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath:
            $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    public override async Task AfterObtained()
    {
        await InspirationMechanics.AddRandom(Owner);
    }

    public override async Task AfterAutoPrePlayPhaseEntered(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner
            || player.PlayerCombatState?.TurnNumber != 1
            || player.Creature.CombatState is not ICombatState combatState)
        {
            return;
        }

        Flash();
        await InspirationMechanics.OfferAtCombatStart(
            player,
            choiceContext,
            combatState);
    }

    public override bool TryModifyRewards(
        Player player,
        List<Reward> rewards,
        AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom)
        {
            return false;
        }

        bool guaranteed =
            room.RoomType is RoomType.Elite or RoomType.Boss;
        if (!guaranteed
            && player.RunState.Rng.Niche.NextFloat() >= 0.25f)
        {
            return false;
        }

        Flash();
        rewards.Add(new InspirationReward(player));
        return true;
    }
}
