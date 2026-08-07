using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class ArchAndRescuePower : ModPowerTemplate
{
    private static bool _enemyIntangibleActive;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.Static(StaticHoverTip.Block)
    ];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/HolyCityEmbracePower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/HolyCityEmbracePower84.png");

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return Amount > 0 && player == Owner.Player;
    }

    public override bool ShouldClearBlock(Creature creature)
    {
        if (creature != Owner || Amount <= 0)
        {
            return true;
        }

        return false;
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner))
        {
            return;
        }

        await ApplyEnemyIntangibleIfNeeded(combatState);

        if (Amount > 0)
        {
            await PowerCmd.Decrement(this);
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player
            || !_enemyIntangibleActive
            || !participants.Contains(Owner))
        {
            return;
        }

        await ClearEnemyIntangible(choiceContext, CombatState);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _enemyIntangibleActive = false;
        return Task.CompletedTask;
    }

    private static async Task ApplyEnemyIntangibleIfNeeded(ICombatState? combatState)
    {
        if (_enemyIntangibleActive || combatState is null)
        {
            return;
        }

        _enemyIntangibleActive = true;
        foreach (Creature enemy in combatState.HittableEnemies.Where(enemy => !enemy.IsDead))
        {
            await PowerCmd.Apply<IntangiblePower>(
                new BlockingPlayerChoiceContext(),
                enemy,
                1,
                enemy,
                null);
        }
    }

    private static async Task ClearEnemyIntangible(
        PlayerChoiceContext choiceContext,
        ICombatState? combatState)
    {
        if (!_enemyIntangibleActive || combatState is null)
        {
            return;
        }

        _enemyIntangibleActive = false;
        foreach (Creature enemy in combatState.HittableEnemies.ToList())
        {
            if (enemy.GetPower<IntangiblePower>() is { } intangible)
            {
                await PowerCmd.Remove(intangible);
            }
        }
    }
}
