using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Saves.Runs;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class AdaptabilityPower : ModPowerTemplate
{
    private int _usedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/LookForFuturePower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/LookForFuturePower84.png");

    [SavedProperty]
    public int UsedThisTurn
    {
        get => _usedThisTurn;
        set
        {
            AssertMutable();
            _usedThisTurn = Math.Max(0, value);
        }
    }

    public static void EnchantRecreatedCards(
        Player owner,
        IReadOnlyList<RecreateResult> results)
    {
        AdaptabilityPower? power =
            owner.Creature.GetPower<AdaptabilityPower>();
        if (power is null)
        {
            return;
        }

        int remaining = Math.Max(0, power.Amount - power.UsedThisTurn);
        foreach (RecreateResult result in results.Take(remaining))
        {
            CardModel card = result.Replacement;
            switch (card.Type)
            {
                case CardType.Attack:
                    CardCmd.Enchant<Sharp>(card, 3);
                    break;
                case CardType.Skill:
                    CardCmd.Enchant<Adroit>(card, 3);
                    break;
                case CardType.Power:
                    CardCmd.Enchant<Sown>(card, 1);
                    break;
            }

            power.UsedThisTurn++;
            power.Flash();
        }
    }

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner.Player)
        {
            UsedThisTurn = 0;
        }

        return Task.CompletedTask;
    }
}
