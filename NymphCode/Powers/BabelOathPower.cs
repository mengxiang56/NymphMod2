using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class BabelOathPower : ModPowerTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/BabelOathPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/BabelOathPower84.png");

    internal static int StrengthGainForNarrations(
        int amount,
        int narrationCount)
    {
        return Math.Max(0, amount) * Math.Max(0, narrationCount);
    }

    internal async Task OnNarrated(
        PlayerChoiceContext choiceContext,
        CardModel cardSource,
        int narrationCount)
    {
        int strengthGain =
            StrengthGainForNarrations(Amount, narrationCount);
        if (cardSource.Owner != Owner.Player || strengthGain <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            strengthGain,
            Owner,
            cardSource);
    }
}
