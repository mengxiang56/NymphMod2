using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class NemesisUndertakerPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/ExtraChapterPower32.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/ExtraChapterPower84.png");

    public async Task OnRecreated(PlayerChoiceContext choiceContext, IReadOnlyList<RecreateResult> results)
    {
        int count = results.Count(result => result.Original.Type == CardType.Attack);
        if (count <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(
            Owner,
            Amount * count,
            ValueProp.Unpowered,
            null);
    }
}