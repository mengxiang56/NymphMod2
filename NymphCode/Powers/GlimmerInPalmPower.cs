using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Nymph.Cards;
using Nymph.Mechanics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class GlimmerInPalmPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/GlimmerInPalmPower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/GlimmerInPalmPower84.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(NymphKeywords.RecreateId),
        HoverTipFactory.FromCard<NymphMiracle>()
    ];

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        for (int i = 0; i < Amount; i++)
        {
            CardModel miracle = CombatState.CreateCard<NymphMiracle>(player);
            await CardPileCmd.AddGeneratedCardToCombat(
                miracle,
                PileType.Hand,
                player);
        }

        await RecreateMechanics.SelectFromHand(
            choiceContext,
            player,
            this,
            0,
            Amount);
    }
}
