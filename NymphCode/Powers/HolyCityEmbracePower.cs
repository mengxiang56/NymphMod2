using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class HolyCityEmbracePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/HolyCityEmbracePower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/HolyCityEmbracePower84.png");

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner.Player)
        {
            await PowerCmd.Remove(this);
        }
    }
}
