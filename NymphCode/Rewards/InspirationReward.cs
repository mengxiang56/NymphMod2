using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using Nymph.Mechanics;
using STS2RitsuLib.Combat.Rewards;

namespace Nymph.Rewards;

public sealed class InspirationReward(Player player) : ModCustomReward(player)
{
    public const string LocalRewardStem = "Inspiration";
    public const string RewardId = "NYMPH_REWARD_INSPIRATION";

    private static readonly BlockingPlayerChoiceContext ChoiceContext = new();

    private CardModel? _offeredCard;

    public override RewardType ModRewardType =>
        ModRewardRegistry.GetRewardType(RewardId);

    public override bool IsPopulated => _offeredCard is not null;

    protected override string? RewardIconPath =>
        InspirationMechanics.PileIconPath;

    protected override string DescriptionLocTable =>
        "static_hover_tips";

    public override void Populate()
    {
        if (_offeredCard is not null)
        {
            return;
        }

        _offeredCard = InspirationMechanics.CreateRandomCard(Player);
    }

    protected override async Task<bool> OnSelect()
    {
        _offeredCard ??= InspirationMechanics.CreateRandomCard(Player);
        CardModel offered = _offeredCard;
        _offeredCard = null;

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            ChoiceContext,
            [offered],
            Player,
            canSkip: true);

        if (selected is null)
        {
            return false;
        }

        await InspirationMechanics.AddToPile(selected);
        return true;
    }

    public override void MarkContentAsSeen()
    {
    }
}
