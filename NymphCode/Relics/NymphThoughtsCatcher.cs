using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Nymph.Characters;
using Nymph.Ftue;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Relics;

[RegisterRelic(typeof(NymphRelicPool))]
public sealed class NymphThoughtsCatcher : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override RelicAssetProfile AssetProfile =>
        NymphRelicAssets.FromFile("思维捕手");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.InspirationId),
    ];

    public override bool IsAllowed(IRunState runState) =>
        runState.Players.Any(player =>
            player.Character is NymphCharacter
            && player.Relics.Any(relic => relic is NymphRelic));

    public override async Task AfterObtained()
    {
        RelicModel? starter = Owner.Relics.FirstOrDefault(relic => relic is NymphRelic);
        if (starter is null)
        {
            return;
        }

        RelicModel upgraded = ModelDb.Relic<NymphThoughtsCatcher>().ToMutable();
        await RelicCmd.Replace(starter, upgraded);

        if (Owner.Relics.Contains(this) && !ReferenceEquals(this, upgraded))
        {
            await RelicCmd.Remove(this);
        }
    }

    public override async Task BeforeCombatStart()
    {
        await InspirationStarterRelicSupport.BeforeCombatStart(Owner);
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

        if (RunManager.Instance.IsSingleplayerOrFakeMultiplayer
            && !SaveManager.Instance.SeenFtue(NymphThoughtFtue.Id))
        {
            NymphThoughtFtueTrigger.ScheduleShow(player);
            return;
        }

        Flash();
        await InspirationStarterRelicSupport.OfferCombatStartInspiration(
            choiceContext,
            player,
            combatState);
    }

    public override bool TryModifyRewards(
        Player player,
        List<Reward> rewards,
        AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        if (!InspirationStarterRelicSupport.TryModifyRewards(
                player,
                rewards,
                room,
                guaranteedDrop: true))
        {
            return false;
        }

        Flash();
        return true;
    }
}
