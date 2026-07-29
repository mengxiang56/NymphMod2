using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class InspirationUsedPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
}

public abstract class InspirationDrawPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected abstract int CardsToDraw { get; }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(choiceContext, CardsToDraw, player);
        await PowerCmd.Decrement(this);
    }
}

[RegisterPower]
public sealed class InspirationOutbloodPower : InspirationDrawPower
{
    protected override int CardsToDraw => 1;
}

[RegisterPower]
public sealed class InspirationFlamesPower : InspirationDrawPower
{
    protected override int CardsToDraw => 2;
}
