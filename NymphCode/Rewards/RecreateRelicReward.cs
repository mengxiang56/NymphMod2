using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using Nymph.Mechanics;
using STS2RitsuLib.Combat.Rewards;

namespace Nymph.Rewards;

public sealed class RecreateRelicReward(Player player) : ModCustomReward(player)
{
    public const string LocalRewardStem = "RecreateRelic";
    public const string RewardId = "NYMPH_REWARD_RECREATE_RELIC";

    public override RewardType ModRewardType =>
        ModRewardRegistry.GetRewardType(RewardId);

    public override bool IsPopulated => true;

    protected override string? RewardIconPath =>
        $"{Entry.ResPath}/images/ui/recreate_relic_reward.png";

    protected override string DescriptionLocTable =>
        "static_hover_tips";

    protected override string DescriptionLocKey =>
        RewardId;

    protected override async Task<bool> OnSelect()
    {
        return await RecreateRelicMechanics.TryRecreateHeldRelic(Player);
    }

    public override void MarkContentAsSeen()
    {
    }
}
