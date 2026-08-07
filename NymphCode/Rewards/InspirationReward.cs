using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using Nymph.Mechanics;
using STS2RitsuLib.Combat.Rewards;

namespace Nymph.Rewards;

public sealed class InspirationReward : ModCustomReward
{
    public const string LocalRewardStem = "Inspiration";
    public const string RewardId = "NYMPH_REWARD_INSPIRATION";

    private static readonly BlockingPlayerChoiceContext ChoiceContext = new();

    private readonly CardRarity? _forcedRarity;
    private ModelId? _predeterminedCardId;
    private CardModel? _offeredCard;

    public InspirationReward(Player player, CardRarity? forcedRarity = null)
        : base(player)
    {
        _forcedRarity = forcedRarity;
    }

    public override RewardType ModRewardType =>
        ModRewardRegistry.GetRewardType(RewardId);

    public override bool IsPopulated => _offeredCard is not null;

    protected override string? RewardIconPath =>
        InspirationMechanics.RewardIconPath;

    protected override string DescriptionLocTable =>
        "static_hover_tips";

    public static InspirationReward FromSavedJson(Player player, string? json)
    {
        CardRarity? rarity = null;
        ModelId? cardId = null;

        if (!string.IsNullOrWhiteSpace(json))
        {
            string[] parts = json.Split('|', 2);
            if (parts.Length == 2)
            {
                if (Enum.TryParse(parts[0], out CardRarity parsedRarity))
                {
                    rarity = parsedRarity;
                }

                cardId = ModelId.Deserialize(parts[1]);
            }
            else
            {
                rarity = ParseForcedRarity(json);
            }
        }

        InspirationReward reward = new(player, rarity);
        if (cardId is ModelId parsedCardId && parsedCardId != ModelId.none)
        {
            reward._predeterminedCardId = parsedCardId;
        }

        return reward;
    }

    public static CardRarity? ParseForcedRarity(string? json)
    {
        return Enum.TryParse(json, out CardRarity rarity) ? rarity : null;
    }

    public override string? ToModRewardJson()
    {
        if (_offeredCard is not null)
        {
            string rarityPart = _forcedRarity?.ToString()
                ?? _offeredCard.Rarity.ToString();
            return $"{rarityPart}|{_offeredCard.Id}";
        }

        return _forcedRarity?.ToString();
    }

    public override void Populate()
    {
        if (_offeredCard is not null)
        {
            return;
        }

        if (_predeterminedCardId is ModelId cardId && cardId != ModelId.none)
        {
            CardModel canonical = ModelDb.GetById<CardModel>(cardId);
            _offeredCard = Player.RunState.CreateCard(canonical, Player);
            return;
        }

        _offeredCard =
            InspirationMechanics.CreateRandomCard(Player, _forcedRarity);
        _predeterminedCardId = _offeredCard.Id;
    }

    protected override async Task<bool> OnSelect()
    {
        Populate();
        CardModel offered = _offeredCard!;

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            ChoiceContext,
            [offered],
            Player,
            canSkip: true);

        if (selected is null)
        {
            return false;
        }

        _offeredCard = null;
        _predeterminedCardId = null;
        await InspirationMechanics.AddToPile(selected);
        return true;
    }

    public override void MarkContentAsSeen()
    {
    }
}
