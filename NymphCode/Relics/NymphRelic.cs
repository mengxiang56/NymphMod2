using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Characters;
using Nymph.Mechanics;
using Nymph.Rewards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
[RegisterCharacterStarterRelic(typeof(NymphCharacter))]
public sealed class NymphRelic : ModRelicTemplate
{
    private int _inspirationRewardOddsPermille =
        InspirationRewardOdds.BaseOddsPermille;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath:
            $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.InspirationId),
    ];

    [SavedProperty]
    public int InspirationRewardOddsPermille
    {
        get => _inspirationRewardOddsPermille;
        set
        {
            AssertMutable();
            _inspirationRewardOddsPermille = value;
        }
    }

    public override async Task AfterObtained()
    {
        await InspirationMechanics.AddRandom(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        await ThoughtMechanics.SyncStatePower(
            new ThrowingPlayerChoiceContext(),
            Owner);
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

        if (room.RoomType == RoomType.Boss
            && IsLastAct(player))
        {
            return false;
        }

        if (room.RoomType == RoomType.Boss)
        {
            Flash();
            rewards.Add(new InspirationReward(player, CardRarity.Rare));
            return true;
        }

        if (room.RoomType is not (RoomType.Monster or RoomType.Elite))
        {
            return false;
        }

        InspirationRewardOdds odds = InspirationRewardOdds.FromPermille(
            InspirationRewardOddsPermille,
            player.PlayerRng.Rewards);
        if (!odds.Roll())
        {
            InspirationRewardOddsPermille = odds.CurrentPermille;
            return false;
        }

        InspirationRewardOddsPermille = odds.CurrentPermille;
        Flash();
        rewards.Add(new InspirationReward(player));
        return true;
    }

    private static bool IsLastAct(Player player) =>
        player.RunState.CurrentActIndex == player.RunState.Acts.Count - 1;
}
