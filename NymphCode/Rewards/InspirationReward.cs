using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using Nymph.Mechanics;
using STS2RitsuLib.Combat.Rewards;

namespace Nymph.Rewards;

public sealed class InspirationReward(Player player) : ModCustomReward(player)
{
    public const string LocalRewardStem = "Inspiration";
    public const string RewardId = "NYMPH_REWARD_INSPIRATION";

    public override RewardType ModRewardType =>
        ModRewardRegistry.GetRewardType(RewardId);

    protected override string? RewardIconPath =>
        InspirationMechanics.PileIconPath;

    protected override string DescriptionLocTable =>
        "static_hover_tips";

    protected override async Task<bool> OnSelect()
    {
        await InspirationMechanics.AddRandom(Player);
        return true;
    }

    public override void MarkContentAsSeen()
    {
    }
}
