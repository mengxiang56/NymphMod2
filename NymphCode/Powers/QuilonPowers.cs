using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Cards;
using Nymph.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

public abstract class QuilonPowerBase : ModPowerTemplate
{
    protected abstract string IconName { get; }
    public override bool AllowNegative => false;
    public override PowerAssetProfile AssetProfile => new(
        $"{Entry.ResPath}/images/powers/{IconName}32.png",
        $"{Entry.ResPath}/images/powers/{IconName}84.png");
}

[RegisterPower]
public sealed class NilaFireStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<NymphNilaFire>();
    protected override bool IsPositive => false;
}

[RegisterPower]
public sealed class TrikayaPower : QuilonPowerBase
{
    private int _remaining = 100;
    protected override string IconName => "TrikayaPower";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override int DisplayAmount => _remaining;
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource
#if !STS2_PUBLIC
        , CardPlay? cardPlay
#endif
    ) => target == Owner ? _remaining : decimal.MaxValue;

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0)
        {
            _remaining = Math.Max(0, _remaining - result.UnblockedDamage);
            InvokeDisplayAmountChanged();
            Flash();
        }
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player && Owner.IsAlive && _remaining < 100)
        {
            _remaining = 100;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class BreathPower : QuilonPowerBase
{
    protected override string IconName => "BreathPower";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner.IsDead) return;
        foreach (var player in combatState.Players)
        {
            CardModel hand = combatState.CreateCard<NymphNilaFire>(player);
            await CardPileCmd.AddGeneratedCardToCombat(hand, PileType.Hand, player);
            CardModel discard = combatState.CreateCard<NymphNilaFire>(player);
            await CardPileCmd.AddGeneratedCardToCombat(discard, PileType.Discard, player);
        }
    }
}

[RegisterPower]
public sealed class NilaFireWardPower : QuilonPowerBase
{
    protected override string IconName => "NilaFireWardPower";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource
#if !STS2_PUBLIC
        , CardPlay? cardPlay
#endif
    )
    {
        if (target != Owner) return 1;
        int fires = CombatState.Players.Sum(p => PileType.Hand.GetPile(p).Cards.Count(c => c is NymphNilaFire));
        return (decimal)Math.Pow(0.5, fires);
    }
}

[RegisterPower]
public sealed class ExhaustFirePower : QuilonPowerBase
{
    private int _turns;
    protected override string IconName => "ExhaustFirePower";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy || ++_turns < 3) return;
        _turns = 0;
        List<CardModel> fires = CombatState.Players.SelectMany(p => PileType.Hand.GetPile(p).Cards).Where(c => c is NymphNilaFire).ToList();
        foreach (CardModel fire in fires) await CardPileCmd.RemoveFromCombat(fire);
        if (fires.Count > 0)
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount * fires.Count, Owner, null);
    }
}

[RegisterPower]
public sealed class ResistPower : QuilonPowerBase
{
    protected override string IconName => "ResistPower";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = target == Owner && canonicalPower.GetTypeForAmount(amount) == PowerType.Debuff
            ? Math.Truncate(amount / 2m)
            : amount;
        return modifiedAmount != amount;
    }
}

[RegisterPower]
public sealed class TransDamagePower : QuilonPowerBase
{
    protected override string IconName => "TransDamagePower";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource
#if !STS2_PUBLIC
        , CardPlay? cardPlay
#endif
    ) => target == Owner && FindChalice() is not null ? .5m : 1m;

    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        Creature? chalice = FindChalice();
        if (target == Owner && chalice is not null && amount > 0)
            await CreatureCmd.Damage(choiceContext, chalice, Math.Ceiling(amount / 2m), ValueProp.Unpowered, Owner, null, null);
    }

    private Creature? FindChalice() => CombatState.Enemies.FirstOrDefault(c => c.IsAlive && c.Monster is ChaliceOfRegret);
}

[RegisterPower]
public sealed class RegretPower : QuilonPowerBase
{
    protected override string IconName => "RegretPower";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
