using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Nymph.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Nymph.Powers;

[RegisterPower]
public sealed class DreadkazEchoPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath:
            $"{Entry.ResPath}/images/powers/DreadkazEchoPower32.png",
        BigIconPath:
            $"{Entry.ResPath}/images/powers/DreadkazEchoPower84.png");

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player
            || cardPlay.Card is NymphCreationReflection)
        {
            return;
        }

        CardModel dreadkaz =
            cardPlay.Card.CombatState!.CreateCard<NymphDreadkaz>(
                cardPlay.Card.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            dreadkaz,
            PileType.Hand,
            Owner.Player);
        await PowerCmd.Decrement(this);
    }
}
